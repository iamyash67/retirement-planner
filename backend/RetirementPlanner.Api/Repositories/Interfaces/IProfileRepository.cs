using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task CreateAsync(UserProfile profile, CancellationToken cancellationToken = default);
    }
}
