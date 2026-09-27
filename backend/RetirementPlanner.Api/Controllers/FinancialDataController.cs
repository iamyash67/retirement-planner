using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.DTO;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/financial")]
    public class FinancialDataController : ControllerBase
    {
        private readonly IContributionService _contributionService;
        private readonly IGoalService _goalService;

        public FinancialDataController(IContributionService contributionService, IGoalService goalService)
        {
            _contributionService = contributionService;
            _goalService = goalService;
        }

        [HttpPost("Add-Investment")]
        public async Task<IActionResult> RecordMonthlyInvestment([FromBody] FinancialDTO request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request body cannot be empty");

            if (request.GoalId <= 0)
                return BadRequest("Invalid Goal ID");

            if (request.Year < 1980 || request.Year > DateTime.Now.Year)
                return BadRequest("Invalid Year");

            if (request.Month < 1 || request.Month > 12)
                return BadRequest("Month must be between 1-12");

            if (request.MonthlyInvestment <= 0)
                return BadRequest("Monthly investment must be positive");

            var result = await _contributionService.RecordAsync(request, cancellationToken);
            return result.Status switch
            {
                ContributionStatus.Recorded => Ok(result.Goal),
                ContributionStatus.GoalNotFound => NotFound("Goal not found"),
                ContributionStatus.ExceedsTarget => BadRequest("Monthly investment cannot exceed target savings"),
                ContributionStatus.AlreadyRecorded => Conflict("Investment already recorded"),
                var status => throw new InvalidOperationException($"Unhandled contribution status {status}.")
            };
        }

        [HttpGet("progress/{goalId}")]
        public async Task<IActionResult> GetProgressByGoalId(int goalId, CancellationToken cancellationToken)
        {
            if (goalId <= 0)
                return BadRequest("Invalid Goal ID");

            var progress = await _goalService.GetProgressAsync(goalId, cancellationToken);
            if (progress == null)
                return NotFound("Goal not found or TargetSavings is zero");

            return Ok(new
            {
                GoalId = goalId,
                Progress = $"{Math.Round(progress.Value, 2)}%"
            });
        }
    }
}
