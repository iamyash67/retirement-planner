using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.DTO;
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
        [HttpGet("{profileId}")]
        public async Task<IActionResult> GetGoal(int profileId, CancellationToken cancellationToken)
        {
            if (profileId <= 0)
                return BadRequest("Invalid Profile ID");

            var goal = await _goalService.GetGoalForUserAsync(profileId, cancellationToken);
            if (goal == null)
                return NotFound("Goal not found");

            return Ok(goal);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGoal([FromBody] GoalDTO newGoal, CancellationToken cancellationToken)
        {
            if (newGoal == null)
                return BadRequest("Goal data cannot be empty");

            if (newGoal.ProfileId <= 0)
                return BadRequest("Invalid Profile ID");

            if (newGoal.CurrentAge <= 0 || newGoal.RetirementAge <= 0)
                return BadRequest("Age values must be positive");

            if (newGoal.CurrentAge >= newGoal.RetirementAge)
                return BadRequest("Retirement age must be greater than current age");

            if (newGoal.TargetSavings <= 0)
                return BadRequest("Target savings must be positive");

            if (newGoal.CurrentSavings >= newGoal.TargetSavings)
                return BadRequest("You have enough savings to reach your goal");

            return await _goalService.CreateGoalAsync(newGoal, cancellationToken) switch
            {
                GoalCreationResult.Created => Ok("Goal created successfully"),
                GoalCreationResult.AlreadyExists => Conflict("A goal already exists for this profile."),
                GoalCreationResult.UserNotFound => NotFound("Profile not found"),
                var result => throw new InvalidOperationException($"Unhandled goal creation result {result}.")
            };
        }
    }
}
