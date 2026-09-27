using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Infrastructure;
using RetirementPlanner.Mapping;
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
        [ProducesResponseType<GoalResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RecordMonthlyInvestment(AddInvestmentRequest request, CancellationToken cancellationToken)
        {
            var result = await _contributionService.RecordAsync(request.ToCommand(), cancellationToken);
            switch (result.Status)
            {
                case ContributionStatus.Recorded:
                    return Ok(result.Goal!.ToResponse());
                case ContributionStatus.GoalNotFound:
                    return NotFound("Goal not found");
                case ContributionStatus.AlreadyRecorded:
                    return Conflict("Investment already recorded");
                case ContributionStatus.ExceedsTarget:
                    // A rule that needs the goal, so the service checks it; reported like any other field error.
                    return FieldErrorResults.ValidationProblem(HttpContext,
                        [(nameof(AddInvestmentRequest.MonthlyInvestment), "Monthly investment cannot exceed target savings")]);
                default:
                    throw new InvalidOperationException($"Unhandled contribution status {result.Status}.");
            }
        }

        [HttpGet("progress/{goalId}")]
        [ProducesResponseType<ProgressResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProgressByGoalId([FromRoute] GetProgressRequest request, CancellationToken cancellationToken)
        {
            var progress = await _goalService.GetProgressAsync(request.GoalId, cancellationToken);
            return progress == null
                ? NotFound("Goal not found or TargetSavings is zero")
                : Ok(progress.Value.ToProgressResponse(request.GoalId));
        }
    }
}
