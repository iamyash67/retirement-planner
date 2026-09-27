using Dapper;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class ContributionRepository : IContributionRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContributionRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExistsAsync(int goalId, int year, int month, CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT EXISTS (
                    SELECT 1 FROM Contributions
                    WHERE GoalId = @GoalId AND `Year` = @Year AND `Month` = @Month)
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(sql, new { GoalId = goalId, Year = year, Month = month }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<int> CreateAsync(Contribution contribution, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO Contributions (GoalId, `Year`, `Month`, Amount, RecordedAt)
                VALUES (@GoalId, @Year, @Month, @Amount, @RecordedAt);
                SELECT LAST_INSERT_ID();
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, contribution, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }
    }
}
