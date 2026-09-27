using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IGoalService
    {
        /// <summary>The user's goal (their most recent one), or null if they have none.</summary>
        Task<Goal?> GetGoalForUserAsync(int userId, CancellationToken cancellationToken = default);
        Task<Goal?> GetGoalAsync(int goalId, CancellationToken cancellationToken = default);
        Task<CreateGoalResult> CreateGoalAsync(CreateGoalCommand command, CancellationToken cancellationToken = default);

        /// <summary>Current savings as a percentage of the target, or null if the goal doesn't exist.</summary>
        Task<decimal?> GetProgressAsync(int goalId, CancellationToken cancellationToken = default);
    }
}
