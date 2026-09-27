using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.Auth;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Infrastructure;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    /// <summary>
    /// The current user's goals. The user id comes only from the access token (User.GetUserId()); a goal
    /// owned by anyone else is reported as 404, exactly like a goal that doesn't exist.
    /// </summary>
    [ApiController]
    [Route("api/goals")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class GoalsController : ControllerBase
    {
        private readonly IGoalService _goalService;
        private readonly IContributionService _contributionService;

        public GoalsController(IGoalService goalService, IContributionService contributionService)
        {
            _goalService = goalService;
            _contributionService = contributionService;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyList<GoalResponse>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> ListGoals(CancellationToken cancellationToken)
        {
            var goals = await _goalService.ListGoalsAsync(User.GetUserId(), cancellationToken);
            return Ok(goals.Select(g => g.ToResponse()).ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType<GoalResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGoal([FromRoute] GoalRouteRequest route, CancellationToken cancellationToken)
        {
            var goal = await _goalService.GetGoalAsync(User.GetUserId(), route.Id, cancellationToken);
            return goal == null ? NotFound("Goal not found") : Ok(goal.ToResponse());
        }

        [HttpPost]
        [ProducesResponseType<GoalResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateGoal(CreateGoalRequest request, CancellationToken cancellationToken)
        {
            var result = await _goalService.CreateGoalAsync(request.ToCommand(User.GetUserId()), cancellationToken);
            return result.Status switch
            {
                GoalCreationStatus.Created => CreatedAtAction(
                    nameof(GetGoal), new { id = result.Goal!.Id }, result.Goal.ToResponse()),
                GoalCreationStatus.AlreadyExists => Conflict("A goal already exists for this user."),
                GoalCreationStatus.UserNotFound => NotFound("User not found"),
                var status => throw new InvalidOperationException($"Unhandled goal creation status {status}.")
            };
        }

        [HttpPost("{id}/contributions")]
        [ProducesResponseType<ContributionResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddContribution(
            [FromRoute] GoalRouteRequest route, AddContributionRequest request, CancellationToken cancellationToken)
        {
            var result = await _contributionService.RecordAsync(
                request.ToCommand(User.GetUserId(), route.Id), cancellationToken);
            return result.Status switch
            {
                ContributionStatus.Recorded => StatusCode(StatusCodes.Status201Created, result.Contribution!.ToResponse()),
                ContributionStatus.GoalNotFound => NotFound("Goal not found"),
                ContributionStatus.AlreadyRecorded => Conflict("A contribution is already recorded for this month"),
                // A rule that needs the goal, so the service checks it; reported like any other field error.
                ContributionStatus.ExceedsTarget => FieldErrorResults.ValidationProblem(HttpContext,
                    [(nameof(AddContributionRequest.Amount), "Amount cannot exceed target savings")]),
                var status => throw new InvalidOperationException($"Unhandled contribution status {status}.")
            };
        }

        [HttpGet("{id}/progress")]
        [ProducesResponseType<ProgressResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProgress([FromRoute] GoalRouteRequest route, CancellationToken cancellationToken)
        {
            var progress = await _goalService.GetProgressAsync(User.GetUserId(), route.Id, cancellationToken);
            return progress == null
                ? NotFound("Goal not found")
                : Ok(progress.Value.ToProgressResponse(route.Id));
        }
    }
}
