using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using RetirementPlanner.Data;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories;
using RetirementPlanner.Services;

namespace RetirementPlanner.Tests.Integration
{
    /// <summary>Services and repositories wired to a real database, as they are for one request.</summary>
    [Collection(MySqlCollection.Name)]
    public class ServiceIntegrationTests(MySqlFixture db)
    {
        [Fact]
        public async Task DevelopmentDataSeeder_IsIdempotent_AndDemoUserCanLogIn()
        {
            await SeedAsync();
            await SeedAsync();

            Assert.Equal(1, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM Users WHERE Email = @email", new { email = DevelopmentDataSeeder.DemoEmail }));

            await using var uow = db.CreateUnitOfWork();
            var userService = new UserService(new UserRepository(uow), new ProfileRepository(uow),
                new PasswordHasher<User>(), TimeProvider.System, NullLogger<UserService>.Instance);

            var user = await userService.AuthenticateAsync(DevelopmentDataSeeder.DemoEmail, DevelopmentDataSeeder.DemoPassword);
            Assert.NotNull(user);
            Assert.Equal("Demo", user.FirstName);
            Assert.Equal(DevelopmentDataSeeder.DemoEmail, user.Email);
            Assert.Null(await userService.AuthenticateAsync(DevelopmentDataSeeder.DemoEmail, "wrong"));
        }

        [Fact]
        public async Task CreateGoal_ThenRecordContribution_UpdatesSavingsAndRejectsSameMonth()
        {
            int userId;
            await using (var setup = db.CreateUnitOfWork())
                userId = await new UserRepository(setup).CreateAsync(MySqlFixture.UniqueEmail(), "hashed");

            await using var uow = db.CreateUnitOfWork();
            var goals = new GoalRepository(uow);
            var goalService = new GoalService(uow, goals, new UserRepository(uow), NullLogger<GoalService>.Instance);
            var contributionService = new ContributionService(uow, goals, new ContributionRepository(uow),
                NullLogger<ContributionService>.Instance);

            var command = new CreateGoalCommand(UserId: userId, CurrentAge: 30, RetirementAge: 60, TargetAmount: 100_000m, CurrentSavings: 10_000m);
            var created = await goalService.CreateGoalAsync(command);
            Assert.Equal(GoalCreationStatus.Created, created.Status);
            Assert.Equal(GoalCreationStatus.AlreadyExists, (await goalService.CreateGoalAsync(command)).Status);
            Assert.Equal(GoalCreationStatus.UserNotFound,
                (await goalService.CreateGoalAsync(command with { UserId = int.MaxValue })).Status);

            var goal = await goalService.GetGoalForUserAsync(userId);
            Assert.NotNull(goal);
            Assert.Equal(created.Goal!.Id, goal.Id);
            Assert.Equal(250m, goal.PlannedMonthlyContribution);

            var investment = new RecordContributionCommand(GoalId: goal.Id, Year: 2026, Month: 9, Amount: 1_000m);
            var recorded = await contributionService.RecordAsync(investment);
            Assert.Equal(ContributionStatus.Recorded, recorded.Status);
            Assert.Equal(11_000m, recorded.Goal!.CurrentSavings);

            var repeated = await contributionService.RecordAsync(investment);
            Assert.Equal(ContributionStatus.AlreadyRecorded, repeated.Status);
            Assert.Equal(11m, await goalService.GetProgressAsync(goal.Id));
        }

        private async Task SeedAsync()
        {
            await using var uow = db.CreateUnitOfWork();
            var seeder = new DevelopmentDataSeeder(uow, new UserRepository(uow), new ProfileRepository(uow),
                new PasswordHasher<User>(), NullLogger<DevelopmentDataSeeder>.Instance);
            await seeder.SeedAsync();
        }
    }
}
