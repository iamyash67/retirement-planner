using MySqlConnector;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories;

namespace RetirementPlanner.Tests.Integration
{
    [Collection(MySqlCollection.Name)]
    public class RepositoryTests(MySqlFixture db)
    {
        [Fact]
        public async Task UserRepository_CreateThenGetByEmail_RoundTrips()
        {
            await using var uow = db.CreateUnitOfWork();
            var repo = new UserRepository(uow);
            var email = MySqlFixture.UniqueEmail();

            var id = await repo.CreateAsync(email, "hashed");
            var user = await repo.GetByEmailAsync(email);

            Assert.NotNull(user);
            Assert.Equal(id, user.Id);
            Assert.Equal("hashed", user.PasswordHash);
            Assert.NotEqual(default, user.CreatedAt);
            Assert.Null(await repo.GetByEmailAsync(MySqlFixture.UniqueEmail()));
        }

        [Fact]
        public async Task UserRepository_TryLock_ReportsWhetherUserExists()
        {
            await using var uow = db.CreateUnitOfWork();
            var repo = new UserRepository(uow);
            var id = await repo.CreateAsync(MySqlFixture.UniqueEmail(), "hashed");

            await uow.BeginAsync();
            Assert.True(await repo.TryLockAsync(id));
            Assert.False(await repo.TryLockAsync(int.MaxValue));
            await uow.RollbackAsync();
        }

        [Fact]
        public async Task UserRepository_DuplicateEmail_ThrowsInsteadOfSwallowing()
        {
            await using var uow = db.CreateUnitOfWork();
            var repo = new UserRepository(uow);
            var email = MySqlFixture.UniqueEmail();
            await repo.CreateAsync(email, "hashed");

            await Assert.ThrowsAsync<MySqlException>(() => repo.CreateAsync(email, "hashed"));
        }

        [Theory]
        [InlineData("Female")]
        [InlineData(null)]
        public async Task ProfileRepository_CreateThenGet_RoundTrips(string? gender)
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var repo = new ProfileRepository(uow);

            await repo.CreateAsync(new UserProfile
            {
                UserId = userId, FirstName = "Ada", LastName = "Lovelace",
                DateOfBirth = new DateOnly(1985, 12, 10), Gender = gender
            });
            var profile = await repo.GetByUserIdAsync(userId);

            Assert.NotNull(profile);
            Assert.Equal("Ada", profile.FirstName);
            Assert.Equal("Lovelace", profile.LastName);
            Assert.Equal(new DateOnly(1985, 12, 10), profile.DateOfBirth);
            Assert.Equal(gender, profile.Gender);
        }

        [Fact]
        public async Task GoalRepository_CreateThenGet_RoundTripsEveryColumn()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var repo = new GoalRepository(uow);

            Assert.False(await repo.ExistsForUserAsync(userId));
            var goalId = await repo.CreateAsync(NewGoal(userId));
            var goal = await repo.GetForUserAsync(goalId, userId);

            Assert.NotNull(goal);
            Assert.Equal(goalId, goal.Id);
            Assert.Equal(userId, goal.UserId);
            Assert.Equal("Retirement", goal.Name);
            Assert.Equal(30, goal.CurrentAge);
            Assert.Equal(60, goal.RetirementAge);
            Assert.Equal(1_000_000m, goal.TargetAmount);
            Assert.Equal(100_000m, goal.CurrentSavings);
            Assert.Equal(0.06m, goal.ExpectedAnnualReturn);
            Assert.Equal(0.12m, goal.ReturnVolatility);
            Assert.Equal(0.025m, goal.InflationRate);
            Assert.Equal(0m, goal.AnnualContributionIncrease);
            Assert.Equal(2_500m, goal.PlannedMonthlyContribution);
            Assert.NotEqual(default, goal.CreatedAt);
            Assert.True(await repo.ExistsForUserAsync(userId));
            Assert.Null(await repo.GetForUserAsync(int.MaxValue, userId));
        }

        [Fact]
        public async Task GoalRepository_OnlyReturnsGoalsOwnedByTheGivenUser()
        {
            await using var uow = db.CreateUnitOfWork();
            var users = new UserRepository(uow);
            var owner = await users.CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var other = await users.CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var repo = new GoalRepository(uow);

            var first = await repo.CreateAsync(NewGoal(owner));
            var second = await repo.CreateAsync(NewGoal(owner));

            Assert.Equal([first, second], (await repo.ListByUserIdAsync(owner)).Select(g => g.Id));
            Assert.Empty(await repo.ListByUserIdAsync(other));
            Assert.Null(await repo.GetForUserAsync(first, other));

            await uow.BeginAsync();
            Assert.NotNull(await repo.GetForUserForUpdateAsync(first, owner));
            Assert.Null(await repo.GetForUserForUpdateAsync(first, other));
            await uow.RollbackAsync();
        }

        [Fact]
        public async Task GoalRepository_CurrentSavingsIncludesContributions()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var goals = new GoalRepository(uow);
            var contributions = new ContributionRepository(uow);
            var goalId = await goals.CreateAsync(NewGoal(userId));

            await contributions.CreateAsync(NewContribution(goalId, 2026, 1, 500m));
            await contributions.CreateAsync(NewContribution(goalId, 2026, 2, 250.50m));

            Assert.Equal(100_750.50m, (await goals.GetForUserAsync(goalId, userId))!.CurrentSavings);
            Assert.Equal(100_750.50m, Assert.Single(await goals.ListByUserIdAsync(userId)).CurrentSavings);
        }

        [Fact]
        public async Task ContributionRepository_ExistsAfterCreate_AndDuplicateThrows()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var goalId = await new GoalRepository(uow).CreateAsync(NewGoal(userId));
            var repo = new ContributionRepository(uow);
            var contribution = NewContribution(goalId, 2026, 3, 100m);

            Assert.False(await repo.ExistsAsync(goalId, 2026, 3));
            Assert.True(await repo.CreateAsync(contribution) > 0);
            Assert.True(await repo.ExistsAsync(goalId, 2026, 3));
            Assert.False(await repo.ExistsAsync(goalId, 2025, 3));

            var ex = await Assert.ThrowsAsync<MySqlException>(() => repo.CreateAsync(contribution));
            Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, ex.ErrorCode);
        }

        internal static Contribution NewContribution(int goalId, int year, int month, decimal amount) => new()
        {
            GoalId = goalId,
            Year = year,
            Month = month,
            Amount = amount,
            RecordedAt = DateTime.UtcNow
        };

        internal static NewGoal NewGoal(int userId) => new()
        {
            UserId = userId,
            Name = "Retirement",
            CurrentAge = 30,
            RetirementAge = 60,
            TargetAmount = 1_000_000m,
            CurrentSavings = 100_000m,
            ExpectedAnnualReturn = 0.06m,
            ReturnVolatility = 0.12m,
            InflationRate = 0.025m,
            AnnualContributionIncrease = 0m,
            PlannedMonthlyContribution = 2_500m
        };
    }
}
