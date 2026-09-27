using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IContributionRepository
    {
        Task<bool> ExistsAsync(int goalId, int year, int month, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(Contribution contribution, CancellationToken cancellationToken = default);
    }
}
