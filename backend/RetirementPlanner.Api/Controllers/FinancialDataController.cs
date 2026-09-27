using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.DTO;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/financial")]
    public class FinancialDataController : ControllerBase
    {
        private readonly IFinancialYearDataService _financialService;
        private readonly IGoalService _goalService;
        private readonly ILogger<FinancialDataController> _logger;

        public FinancialDataController(
            IFinancialYearDataService financialService,
            IGoalService goalService,
            ILogger<FinancialDataController> logger)
        {
            _financialService = financialService;
            _goalService = goalService;
            _logger = logger;
        }

        [HttpPost("Add-Investment")]
        public async Task<IActionResult> RecordMonthlyInvestment([FromBody] FinancialDTO request)
        {
            _logger.LogInformation("Starting RecordMonthlyInvestment for GoalId: {GoalId}", request?.GoalId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("Empty request body received");
                    return BadRequest("Request body cannot be empty");
                }

                _logger.LogDebug("Validating parameters - GoalId: {GoalId}, Year: {Year}, Month: {Month}",
                    request.GoalId, request.Year, request.Month);

                // Parameter validation
                if (request.GoalId <= 0)
                    return BadRequest("Invalid Goal ID");

                if (request.Year < 1980 || request.Year > DateTime.Now.Year)
                    return BadRequest("Invalid Year");

                if (request.Month < 1 || request.Month > 12)
                    return BadRequest("Month must be between 1-12");

                if (request.MonthlyInvestment <= 0)
                    return BadRequest("Monthly investment must be positive");

                // Get ProfileId from GoalId
                var profileId = await _goalService.GetProfileIdByGoalIdAsync(request.GoalId);
                if (profileId == null)
                {
                    _logger.LogWarning("ProfileId not found for GoalId: {GoalId}", request.GoalId);
                    return NotFound("Goal not found");
                }
                var goal = await _goalService.GetGoalByIdAsync(profileId.Value);
                if (goal == null)
                {
                    _logger.LogWarning("Goal not found for ProfileId: {ProfileId}", profileId);
                    return NotFound("Goal not found");
                }

                if (request.MonthlyInvestment > goal.TargetSavings)
                {
                    _logger.LogWarning("Monthly investment {MonthlyInvestment} exceeds target savings {TargetSavings} for GoalId: {GoalId}",
                        request.MonthlyInvestment, goal.TargetSavings, request.GoalId);
                    return BadRequest("Monthly investment cannot exceed target savings");
                }

                using var transaction = _financialService.BeginTransaction();
                try
                {
                    var financialData = await _financialService.GetFinancialDataAsync(
                        request.GoalId, request.Year, request.Month);

                    if (financialData == null)
                    {
                        _logger.LogInformation("Creating new investment plan for GoalId: {GoalId}", request.GoalId);
                        var newPlan = new FinancialYearData
                        {
                            GoalId = request.GoalId,
                            Year = request.Year,
                            Month = request.Month,
                            MonthlyInvestment = request.MonthlyInvestment
                        };

                        financialData = await _financialService.CreateOrUpdateFinancialDataAsync(newPlan);
                        if (financialData == null)
                        {
                            _logger.LogError("Failed to create investment plan for GoalId: {GoalId}", request.GoalId);
                            transaction.Rollback();
                            return StatusCode(500, "Failed to create investment plan");
                        }
                    }

                    if (financialData.IsInvested)
                    {
                        _logger.LogWarning("Duplicate investment attempt for RecordId: {RecordId}", financialData.Id);
                        transaction.Rollback();
                        return Conflict("Investment already recorded");
                    }

                    if (!await _financialService.RecordInvestmentAsync(financialData.Id))
                    {
                        _logger.LogError("Investment recording failed for RecordId: {RecordId}", financialData.Id);
                        transaction.Rollback();
                        return StatusCode(500, "Failed to record investment");
                    }

                    _logger.LogInformation("Investment recorded successfully for RecordId: {RecordId}", financialData.Id);
                    transaction.Commit();
                    var updatedGoal = await _goalService.GetGoalByIdAsync(profileId.Value);
                    if (updatedGoal == null)
                    {
                        _logger.LogWarning("Failed to fetch updated goal after investment for ProfileId: {ProfileId}", profileId);
                        return StatusCode(500, "Failed to retrieve updated goal");
                    }

                    return Ok(updatedGoal);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction failed for GoalId: {GoalId}", request.GoalId);
                    transaction.Rollback();
                    return StatusCode(500, $"Transaction failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in RecordMonthlyInvestment");
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [HttpGet("progress/{goalId}")]
        public async Task<IActionResult> GetProgressByGoalId(int goalId)
        {
            if (goalId <= 0)
                return BadRequest("Invalid Goal ID");

            try
            {
                var progress = await _goalService.GetGoalProgressByGoalIdAsync(goalId);
                if (progress == null)
                    return NotFound("Goal not found or TargetSavings is zero");

                return Ok(new
                {
                    GoalId = goalId,
                    Progress = $"{Math.Round(progress.Value, 2)}%"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for GoalId: {goalId}", goalId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}