using Dapper;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO RefreshTokens (UserId, FamilyId, TokenHash, CreatedAt, ExpiresAt)
                VALUES (@UserId, @FamilyId, @TokenHash, @CreatedAt, @ExpiresAt);
                SELECT LAST_INSERT_ID();
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, token, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<RefreshToken?> GetByHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            const string sql = """
                SELECT Id, UserId, FamilyId, TokenHash, CreatedAt, ExpiresAt, RevokedAt, ReplacedByTokenId
                FROM RefreshTokens
                WHERE TokenHash = @TokenHash
                FOR UPDATE
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<RefreshToken>(
                new CommandDefinition(sql, new { TokenHash = tokenHash }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task RevokeAsync(int tokenId, DateTime revokedAt, int? replacedByTokenId, CancellationToken cancellationToken = default)
        {
            const string sql = """
                UPDATE RefreshTokens
                SET RevokedAt = @RevokedAt, ReplacedByTokenId = @ReplacedByTokenId
                WHERE Id = @TokenId AND RevokedAt IS NULL
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(sql,
                new { TokenId = tokenId, RevokedAt = revokedAt, ReplacedByTokenId = replacedByTokenId },
                _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<int> RevokeFamilyAsync(Guid familyId, DateTime revokedAt, CancellationToken cancellationToken = default)
        {
            const string sql = """
                UPDATE RefreshTokens
                SET RevokedAt = @RevokedAt
                WHERE FamilyId = @FamilyId AND RevokedAt IS NULL
                """;

            var connection = await _unitOfWork.GetConnectionAsync(cancellationToken);
            return await connection.ExecuteAsync(new CommandDefinition(sql,
                new { FamilyId = familyId, RevokedAt = revokedAt }, _unitOfWork.Transaction, cancellationToken: cancellationToken));
        }
    }
}
