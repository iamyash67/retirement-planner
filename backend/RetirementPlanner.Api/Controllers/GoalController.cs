using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/goal")]
    public class GoalController : ControllerBase
    {
        private readonly IGoalService _goalService;

        public GoalController(IGoalService goalService)
        {
            _goalService = goalService;
        }

        /// <summary>Gets the goal of the profile (user) with the given id.</summary>
        [HttpGet("{profileId}", Name = nameof(GetGoal))]
        [ProducesResponseType<GoalResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGoal([FromRoute] GetGoalRequest request, CancellationToken cancellationToken)
        {
            var goal = await _goalService.GetGoalForUserAsync(request.ProfileId, cancellationToken);
            return goal == null ? NotFound("Goal not found") : Ok(goal.ToResponse());
        }

        [HttpPost]
        [ProducesResponseType<GoalResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateGoal(CreateGoalRequest request, CancellationToken cancellationToken)
        {
            var result = await _goalService.CreateGoalAsync(request.ToCommand(), cancellationToken);
            return result.Status switch
            {
                GoalCreationStatus.Created => CreatedAtRoute(
                    nameof(GetGoal), new { profileId = request.ProfileId }, result.Goal!.ToResponse()),
                GoalCreationStatus.AlreadyExists => Conflict("A goal already exists for this profile."),
                GoalCreationStatus.UserNotFound => NotFound("Profile not found"),
                var status => throw new InvalidOperationException($"Unhandled goal creation status {status}.")
            };
        }
    }
}
