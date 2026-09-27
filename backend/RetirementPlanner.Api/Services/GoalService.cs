using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.DTO;

namespace RetirementPlanner.Services
{
    public class GoalService : IGoalService
    {
        private readonly IGoalRepository _goalRepo;
        private readonly ILogger<GoalService> _logger;

        public GoalService(IGoalRepository goalRepo, ILogger<GoalService> logger)
        {
            _goalRepo = goalRepo;
            _logger = logger;
        }

        public async Task<Goal?> GetGoalByIdAsync(int id)
        {
            _logger.LogInformation("Fetching goal with ID: {ProfileId}", id);
            try
            {
                var goal = await _goalRepo.GetByProfileIdAsync(id);
                if (goal == null)
                {
                    _logger.LogWarning("Goal with ID {ProfileId} not found", id);
                }
                return goal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goal with ID: {ProfileId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByProfileIdAsync(int profileId)
        {
            _logger.LogInformation("Checking if profile exists: {ProfileId}", profileId);
            try
            {
                return await _goalRepo.ExistsByProfileIdAsync(profileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for ProfileId: {ProfileId}", profileId);
                throw;
            }
        }

        public async Task<bool> GoalExistsAsync(int goalId)
        {
            _logger.LogInformation("Checking if goal exists: {GoalId}", goalId);
            try
            {
                return await _goalRepo.ExistsByGoalIdAsync(goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for GoalId: {GoalId}", goalId);
                throw;
            }
        }

        public async Task<bool> CreateGoalAsync(GoalDTO goal)
        {
            _logger.LogInformation("Creating new goal for ProfileId: {ProfileId}", goal.ProfileId);
            try
            {
                var result = await _goalRepo.CreateAsync(goal);
                if (!result)
                {
                    _logger.LogWarning("Failed to create goal for ProfileId: {ProfileId}", goal.ProfileId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for ProfileId: {ProfileId}", goal.ProfileId);
                return false;
            }
        }

        public async Task<int?> GetProfileIdByGoalIdAsync(int goalId)
        {
            return await _goalRepo.GetProfileIdByGoalIdAsync(goalId);
        }

        public async Task<decimal?> GetGoalProgressByGoalIdAsync(int goalId)
        {
            _logger.LogInformation("Calculating progress for GoalId: {GoalId}", goalId);
            try
            {
                var goal = await _goalRepo.GetByGoalIdAsync(goalId);
                if (goal == null || goal.TargetSavings == 0)
                {
                    _logger.LogWarning("Goal not found or TargetSavings is zero for GoalId: {GoalId}", goalId);
                    return null;
                }

                var totalSavings = await _goalRepo.GetUserProgressAsync(goalId);
                if (totalSavings == null)
                {
                    _logger.LogWarning("No progress data found for GoalId: {GoalId}", goalId);
                    return null;
                }

                return (totalSavings.Value / goal.TargetSavings) * 100;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating progress for GoalId: {GoalId}", goalId);
                throw;
            }
        }
    }
}