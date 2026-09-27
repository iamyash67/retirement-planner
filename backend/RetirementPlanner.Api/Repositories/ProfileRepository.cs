using Dapper;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProfileRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT UserId, FirstName, LastName, DateOfBirth, Gender
                FROM Profiles
                WHERE UserId = @UserId
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<UserProfile>(
                new CommandDefinition(sql, new { UserId = userId }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task CreateAsync(UserProfile profile, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO Profiles (UserId, FirstName, LastName, DateOfBirth, Gender)
                VALUES (@UserId, @FirstName, @LastName, @DateOfBirth, @Gender)
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(
                new CommandDefinition(sql, profile, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }
    }
}
