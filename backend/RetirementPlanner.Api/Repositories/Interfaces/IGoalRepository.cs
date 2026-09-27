using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IGoalRepository
    {
        /// <summary>The user's most recently created goal, or null if they have none.</summary>
        Task<Goal?> GetLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<Goal?> GetByIdAsync(int goalId, CancellationToken cancellationToken = default);

        /// <summary>Like <see cref="GetByIdAsync"/>, but locks the goal's row until the current transaction ends.</summary>
        Task<Goal?> GetByIdForUpdateAsync(int goalId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(NewGoal goal, CancellationToken cancellationToken = default);
    }
}
