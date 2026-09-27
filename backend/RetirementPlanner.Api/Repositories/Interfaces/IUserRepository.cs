using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(string email, string passwordHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Locks the user's row until the current transaction ends. Returns false when the user doesn't exist.
        /// </summary>
        Task<bool> TryLockAsync(int userId, CancellationToken cancellationToken = default);
    }
}
