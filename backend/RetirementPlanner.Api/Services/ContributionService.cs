using MySqlConnector;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.DTO;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Services
{
    public class ContributionService : IContributionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGoalRepository _goalRepo;
        private readonly IContributionRepository _contributionRepo;
        private readonly ILogger<ContributionService> _logger;

        public ContributionService(
            IUnitOfWork unitOfWork,
            IGoalRepository goalRepo,
            IContributionRepository contributionRepo,
            ILogger<ContributionService> logger)
        {
            _unitOfWork = unitOfWork;
            _goalRepo = goalRepo;
            _contributionRepo = contributionRepo;
            _logger = logger;
        }

        public async Task<ContributionResult> RecordAsync(FinancialDTO request, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(
                    () => RecordInTransactionAsync(request, cancellationToken), cancellationToken);
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                // The unique key (GoalId, Year, Month) rejected the insert: another request recorded this
                // month first. The unit of work has already rolled back, so nothing from this request was
                // written. The contribution insert is the only write in this flow that can hit a unique key.
                _logger.LogInformation(ex, "Contribution for goal {GoalId}, {Year}-{Month:00} was recorded concurrently",
                    request.GoalId, request.Year, request.Month);
                return new ContributionResult(ContributionStatus.AlreadyRecorded);
            }
        }

        private async Task<ContributionResult> RecordInTransactionAsync(FinancialDTO request, CancellationToken cancellationToken)
        {
            // Locking the goal serialises concurrent requests for it, so the duplicate check below normally
            // sees any contribution committed before it. The unique key is the backstop if it doesn't.
            var goal = await _goalRepo.GetByIdForUpdateAsync(request.GoalId, cancellationToken);
            if (goal == null)
                return new ContributionResult(ContributionStatus.GoalNotFound);

            if (request.MonthlyInvestment > goal.TargetSavings)
                return new ContributionResult(ContributionStatus.ExceedsTarget);

            if (await _contributionRepo.ExistsAsync(request.GoalId, request.Year, request.Month, cancellationToken))
                return new ContributionResult(ContributionStatus.AlreadyRecorded);

            await _contributionRepo.CreateAsync(new Contribution
            {
                GoalId = request.GoalId,
                Year = request.Year,
                Month = request.Month,
                Amount = request.MonthlyInvestment
            }, cancellationToken);

            _logger.LogInformation("Recorded contribution for goal {GoalId}, {Year}-{Month:00}",
                request.GoalId, request.Year, request.Month);

            var updatedGoal = await _goalRepo.GetByIdAsync(request.GoalId, cancellationToken)
                ?? throw new InvalidOperationException($"Goal {request.GoalId} disappeared inside its own transaction.");
            return new ContributionResult(ContributionStatus.Recorded, updatedGoal);
        }
    }
}
