using Dapper;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class GoalRepository : IGoalRepository
    {
        // Maps the Goals table onto the API's Goal shape. CurrentSavings is the savings entered at
        // creation plus all recorded contributions, so the total can never drift from the contributions.
        private const string SelectGoal = """
            SELECT g.Id                         AS GoalId,
                   g.UserId                     AS ProfileId,
                   g.CurrentAge,
                   g.RetirementAge,
                   g.TargetAmount               AS TargetSavings,
                   g.PlannedMonthlyContribution AS MonthlyContribution,
                   g.CurrentSavings + COALESCE(
                       (SELECT SUM(c.Amount) FROM Contributions c WHERE c.GoalId = g.Id), 0) AS CurrentSavings
            FROM Goals g
            """;

        private readonly IUnitOfWork _unitOfWork;

        public GoalRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Goal?> GetLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
            QuerySingleOrDefaultAsync(
                $"{SelectGoal} WHERE g.UserId = @UserId ORDER BY g.CreatedAt DESC, g.Id DESC LIMIT 1",
                new { UserId = userId }, cancellationToken);

        public Task<Goal?> GetByIdAsync(int goalId, CancellationToken cancellationToken = default) =>
            QuerySingleOrDefaultAsync($"{SelectGoal} WHERE g.Id = @GoalId", new { GoalId = goalId }, cancellationToken);

        // FOR UPDATE on the outer query locks only the Goals row, not the contributions read by the subquery.
        public Task<Goal?> GetByIdForUpdateAsync(int goalId, CancellationToken cancellationToken = default) =>
            QuerySingleOrDefaultAsync($"{SelectGoal} WHERE g.Id = @GoalId FOR UPDATE", new { GoalId = goalId }, cancellationToken);

        public async Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM Goals WHERE UserId = @UserId)";

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { UserId = userId }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<int> CreateAsync(NewGoal goal, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO Goals (UserId, Name, CurrentAge, RetirementAge, TargetAmount, CurrentSavings,
                                   ExpectedAnnualReturn, ReturnVolatility, InflationRate,
                                   AnnualContributionIncrease, PlannedMonthlyContribution)
                VALUES (@UserId, @Name, @CurrentAge, @RetirementAge, @TargetAmount, @CurrentSavings,
                        @ExpectedAnnualReturn, @ReturnVolatility, @InflationRate,
                        @AnnualContributionIncrease, @PlannedMonthlyContribution);
                SELECT LAST_INSERT_ID();
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, goal, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        private async Task<Goal?> QuerySingleOrDefaultAsync(string sql, object parameters, CancellationToken cancellationToken)
        {
            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<Goal>(
                new CommandDefinition(sql, parameters, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }
    }
}
