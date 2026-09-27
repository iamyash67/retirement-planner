using Dapper;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash, CreatedAt
                FROM Users
                WHERE Email = @Email
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(sql, new { Email = email }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT Id, Email, PasswordHash, CreatedAt
                FROM Users
                WHERE Id = @UserId
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(sql, new { UserId = userId }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<int> CreateAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO Users (Email, PasswordHash) VALUES (@Email, @PasswordHash);
                SELECT LAST_INSERT_ID();
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, new { Email = email, PasswordHash = passwordHash }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<bool> TryLockAsync(int userId, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT Id FROM Users WHERE Id = @UserId FOR UPDATE";

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            var id = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(sql, new { UserId = userId }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
            return id.HasValue;
        }
    }
}
