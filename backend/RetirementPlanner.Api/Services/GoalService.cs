using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.DTO;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Services
{
    public class GoalService : IGoalService
    {
        // Used when the request doesn't provide the simulation inputs.
        public const string DefaultName = "Retirement";
        public const decimal DefaultExpectedAnnualReturn = 0.06m;
        public const decimal DefaultReturnVolatility = 0.12m;
        public const decimal DefaultInflationRate = 0.025m;
        public const decimal DefaultAnnualContributionIncrease = 0m;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoalRepository _goalRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<GoalService> _logger;

        public GoalService(
            IUnitOfWork unitOfWork,
            IGoalRepository goalRepo,
            IUserRepository userRepo,
            ILogger<GoalService> logger)
        {
            _unitOfWork = unitOfWork;
            _goalRepo = goalRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        public Task<Goal?> GetGoalForUserAsync(int userId, CancellationToken cancellationToken = default) =>
            _goalRepo.GetLatestByUserIdAsync(userId, cancellationToken);

        public Task<Goal?> GetGoalAsync(int goalId, CancellationToken cancellationToken = default) =>
            _goalRepo.GetByIdAsync(goalId, cancellationToken);

        public Task<GoalCreationResult> CreateGoalAsync(GoalDTO goal, CancellationToken cancellationToken = default) =>
            _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Locking the user serialises concurrent requests, so the one-goal check below can't race.
                if (!await _userRepo.TryLockAsync(goal.ProfileId, cancellationToken))
                    return GoalCreationResult.UserNotFound;

                if (await _goalRepo.ExistsForUserAsync(goal.ProfileId, cancellationToken))
                    return GoalCreationResult.AlreadyExists;

                var goalId = await _goalRepo.CreateAsync(new NewGoal
                {
                    UserId = goal.ProfileId,
                    Name = string.IsNullOrWhiteSpace(goal.Name) ? DefaultName : goal.Name.Trim(),
                    CurrentAge = goal.CurrentAge,
                    RetirementAge = goal.RetirementAge,
                    TargetAmount = goal.TargetSavings,
                    CurrentSavings = goal.CurrentSavings,
                    ExpectedAnnualReturn = goal.ExpectedAnnualReturn ?? DefaultExpectedAnnualReturn,
                    ReturnVolatility = goal.ReturnVolatility ?? DefaultReturnVolatility,
                    InflationRate = goal.InflationRate ?? DefaultInflationRate,
                    AnnualContributionIncrease = goal.AnnualContributionIncrease ?? DefaultAnnualContributionIncrease,
                    PlannedMonthlyContribution = CalculateMonthlyContribution(
                        goal.TargetSavings, goal.CurrentSavings, goal.CurrentAge, goal.RetirementAge)
                }, cancellationToken);

                _logger.LogInformation("Created goal {GoalId} for user {UserId}", goalId, goal.ProfileId);
                return GoalCreationResult.Created;
            }, cancellationToken);

        public async Task<decimal?> GetProgressAsync(int goalId, CancellationToken cancellationToken = default)
        {
            var goal = await _goalRepo.GetByIdAsync(goalId, cancellationToken);
            if (goal == null || goal.TargetSavings == 0)
                return null;

            return goal.CurrentSavings / goal.TargetSavings * 100;
        }

        /// <summary>
        /// What must be saved each month to close the gap by retirement, ignoring returns.
        /// Rounds half away from zero, as MySQL's ROUND did in the old CreateGoal procedure.
        /// </summary>
        public static decimal CalculateMonthlyContribution(
            decimal targetSavings, decimal currentSavings, int currentAge, int retirementAge)
        {
            var months = (retirementAge - currentAge) * 12;
            if (months <= 0)
                throw new ArgumentException("Retirement age must be greater than current age.");

            return Math.Round((targetSavings - currentSavings) / months, 2, MidpointRounding.AwayFromZero);
        }
    }
}
