using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    /// <summary>Every method is scoped to one user: goals of other users are never returned.</summary>
    public interface IGoalService
    {
        Task<IReadOnlyList<Goal>> ListGoalsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>The goal, or null if it doesn't exist or belongs to another user.</summary>
        Task<Goal?> GetGoalAsync(int userId, int goalId, CancellationToken cancellationToken = default);
        Task<CreateGoalResult> CreateGoalAsync(CreateGoalCommand command, CancellationToken cancellationToken = default);

        /// <summary>Current savings as a percentage of the target, or null if the user has no such goal.</summary>
        Task<decimal?> GetProgressAsync(int userId, int goalId, CancellationToken cancellationToken = default);
    }
}
