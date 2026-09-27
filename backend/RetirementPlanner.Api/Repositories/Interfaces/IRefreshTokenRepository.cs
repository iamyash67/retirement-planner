using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<int> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);

        /// <summary>Finds a token by its hash and locks the row until the current transaction ends.</summary>
        Task<RefreshToken?> GetByHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);

        Task RevokeAsync(int tokenId, DateTime revokedAt, int? replacedByTokenId, CancellationToken cancellationToken = default);

        /// <summary>Revokes every still-active token in the family. Returns how many were revoked.</summary>
        Task<int> RevokeFamilyAsync(Guid familyId, DateTime revokedAt, CancellationToken cancellationToken = default);
    }
}
