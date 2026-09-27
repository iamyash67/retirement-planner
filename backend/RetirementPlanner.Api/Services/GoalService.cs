using RetirementPlanner.Data.Interfaces;
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

        public Task<IReadOnlyList<Goal>> ListGoalsAsync(int userId, CancellationToken cancellationToken = default) =>
            _goalRepo.ListByUserIdAsync(userId, cancellationToken);

        public Task<Goal?> GetGoalAsync(int userId, int goalId, CancellationToken cancellationToken = default) =>
            _goalRepo.GetForUserAsync(goalId, userId, cancellationToken);

        public Task<CreateGoalResult> CreateGoalAsync(CreateGoalCommand command, CancellationToken cancellationToken = default) =>
            _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Locking the user serialises concurrent requests, so the one-goal check below can't race.
                if (!await _userRepo.TryLockAsync(command.UserId, cancellationToken))
                    return new CreateGoalResult(GoalCreationStatus.UserNotFound);

                if (await _goalRepo.ExistsForUserAsync(command.UserId, cancellationToken))
                    return new CreateGoalResult(GoalCreationStatus.AlreadyExists);

                var goalId = await _goalRepo.CreateAsync(new NewGoal
                {
                    UserId = command.UserId,
                    Name = string.IsNullOrWhiteSpace(command.Name) ? DefaultName : command.Name.Trim(),
                    CurrentAge = command.CurrentAge,
                    RetirementAge = command.RetirementAge,
                    TargetAmount = command.TargetAmount,
                    CurrentSavings = command.CurrentSavings,
                    ExpectedAnnualReturn = command.ExpectedAnnualReturn ?? DefaultExpectedAnnualReturn,
                    ReturnVolatility = command.ReturnVolatility ?? DefaultReturnVolatility,
                    InflationRate = command.InflationRate ?? DefaultInflationRate,
                    AnnualContributionIncrease = command.AnnualContributionIncrease ?? DefaultAnnualContributionIncrease,
                    PlannedMonthlyContribution = CalculateMonthlyContribution(
                        command.TargetAmount, command.CurrentSavings, command.CurrentAge, command.RetirementAge)
                }, cancellationToken);

                _logger.LogInformation("Created goal {GoalId} for user {UserId}", goalId, command.UserId);

                var goal = await _goalRepo.GetForUserAsync(goalId, command.UserId, cancellationToken)
                    ?? throw new InvalidOperationException($"Goal {goalId} disappeared inside its own transaction.");
                return new CreateGoalResult(GoalCreationStatus.Created, goal);
            }, cancellationToken);

        public async Task<decimal?> GetProgressAsync(int userId, int goalId, CancellationToken cancellationToken = default)
        {
            var goal = await _goalRepo.GetForUserAsync(goalId, userId, cancellationToken);
            if (goal == null || goal.TargetAmount == 0)
                return null;

            return goal.CurrentSavings / goal.TargetAmount * 100;
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
