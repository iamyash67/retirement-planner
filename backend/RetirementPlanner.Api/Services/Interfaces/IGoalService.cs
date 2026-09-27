using RetirementPlanner.Models;
using RetirementPlanner.DTO;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IGoalService
    {
        Task<Goal?> GetGoalByIdAsync(int id);
        Task<bool> GoalExistsAsync(int goalId);
        Task<bool> ExistsByProfileIdAsync(int profileId);
        Task<bool> CreateGoalAsync(GoalDTO goal);
        Task<int?> GetProfileIdByGoalIdAsync(int goalId);
        Task<decimal?> GetGoalProgressByGoalIdAsync(int goalId);
    }
}