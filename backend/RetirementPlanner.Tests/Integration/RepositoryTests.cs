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
        public async Task GoalRepository_CreateThenGet_MapsToApiShape()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var repo = new GoalRepository(uow);

            Assert.False(await repo.ExistsForUserAsync(userId));
            var goalId = await repo.CreateAsync(NewGoal(userId));
            var goal = await repo.GetByIdAsync(goalId);

            Assert.NotNull(goal);
            Assert.Equal(goalId, goal.GoalId);
            Assert.Equal(userId, goal.ProfileId);
            Assert.Equal(30, goal.CurrentAge);
            Assert.Equal(60, goal.RetirementAge);
            Assert.Equal(1_000_000m, goal.TargetSavings);
            Assert.Equal(2_500m, goal.MonthlyContribution);
            Assert.Equal(100_000m, goal.CurrentSavings);
            Assert.True(await repo.ExistsForUserAsync(userId));
            Assert.Null(await repo.GetByIdAsync(int.MaxValue));
        }

        [Fact]
        public async Task GoalRepository_GetLatestByUserId_ReturnsNewestGoal()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var repo = new GoalRepository(uow);

            await repo.CreateAsync(NewGoal(userId));
            var newest = await repo.CreateAsync(NewGoal(userId));

            Assert.Equal(newest, (await repo.GetLatestByUserIdAsync(userId))!.GoalId);
            Assert.Null(await repo.GetLatestByUserIdAsync(int.MaxValue));
        }

        [Fact]
        public async Task GoalRepository_CurrentSavingsIncludesContributions()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var goals = new GoalRepository(uow);
            var contributions = new ContributionRepository(uow);
            var goalId = await goals.CreateAsync(NewGoal(userId));

            await contributions.CreateAsync(new Contribution { GoalId = goalId, Year = 2026, Month = 1, Amount = 500m });
            await contributions.CreateAsync(new Contribution { GoalId = goalId, Year = 2026, Month = 2, Amount = 250.50m });

            Assert.Equal(100_750.50m, (await goals.GetByIdAsync(goalId))!.CurrentSavings);
            Assert.Equal(100_750.50m, (await goals.GetLatestByUserIdAsync(userId))!.CurrentSavings);
        }

        [Fact]
        public async Task ContributionRepository_ExistsAfterCreate_AndDuplicateThrows()
        {
            await using var uow = db.CreateUnitOfWork();
            var userId = await new UserRepository(uow).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            var goalId = await new GoalRepository(uow).CreateAsync(NewGoal(userId));
            var repo = new ContributionRepository(uow);
            var contribution = new Contribution { GoalId = goalId, Year = 2026, Month = 3, Amount = 100m };

            Assert.False(await repo.ExistsAsync(goalId, 2026, 3));
            Assert.True(await repo.CreateAsync(contribution) > 0);
            Assert.True(await repo.ExistsAsync(goalId, 2026, 3));
            Assert.False(await repo.ExistsAsync(goalId, 2025, 3));

            var ex = await Assert.ThrowsAsync<MySqlException>(() => repo.CreateAsync(contribution));
            Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, ex.ErrorCode);
        }

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
