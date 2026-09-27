using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    /// <summary>
    /// Goal queries always take the owning user's id, so a goal that belongs to someone else is
    /// indistinguishable from one that doesn't exist.
    /// </summary>
    public interface IGoalRepository
    {
        /// <summary>The user's goals, oldest first.</summary>
        Task<IReadOnlyList<Goal>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<Goal?> GetForUserAsync(int goalId, int userId, CancellationToken cancellationToken = default);

        /// <summary>Like <see cref="GetForUserAsync"/>, but locks the goal's row until the current transaction ends.</summary>
        Task<Goal?> GetForUserForUpdateAsync(int goalId, int userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(NewGoal goal, CancellationToken cancellationToken = default);
    }
}
