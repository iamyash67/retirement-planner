using Microsoft.Extensions.DependencyInjection;
using RetirementPlanner.DTO;
using RetirementPlanner.Infrastructure;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Tests.Integration
{
    /// <summary>
    /// Many requests recording the same month at once, each in its own DI scope (and so its own
    /// unit of work and connection), resolved through the same registrations the API uses.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class ConcurrentContributionTests(MySqlFixture db)
    {
        private const int Requests = 10;

        [Fact]
        public async Task RecordAsync_ConcurrentRequestsForSameMonth_RecordExactlyOnce()
        {
            await using var provider = new ServiceCollection()
                .AddLogging()
                .AddRetirementPlanner(db.ConnectionString)
                .BuildServiceProvider(validateScopes: true);

            var goalId = await CreateGoalAsync(provider);
            var request = new FinancialDTO { GoalId = goalId, Year = 2026, Month = 7, MonthlyInvestment = 100m };

            // Start every request before awaiting any, so they overlap on the database.
            using var start = new ManualResetEventSlim();
            var tasks = Enumerable.Range(0, Requests).Select(_ => Task.Run(async () =>
            {
                start.Wait();
                await using var scope = provider.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<IContributionService>().RecordAsync(request);
            })).ToArray();
            start.Set();

            // Task.WhenAll rethrows if any request threw, which fails the test.
            var results = await Task.WhenAll(tasks);

            Assert.Equal(1, results.Count(r => r.Status == ContributionStatus.Recorded));
            Assert.Equal(Requests - 1, results.Count(r => r.Status == ContributionStatus.AlreadyRecorded));
            Assert.Equal(1, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM Contributions WHERE GoalId = @goalId AND `Year` = 2026 AND `Month` = 7",
                new { goalId }));
            Assert.Equal(100_100m, results.Single(r => r.Status == ContributionStatus.Recorded).Goal!.CurrentSavings);
        }

        private static async Task<int> CreateGoalAsync(IServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var userId = await scope.ServiceProvider.GetRequiredService<IUserRepository>()
                .CreateAsync(MySqlFixture.UniqueEmail(), "hashed");
            return await scope.ServiceProvider.GetRequiredService<IGoalRepository>()
                .CreateAsync(RepositoryTests.NewGoal(userId));
        }
    }
}
