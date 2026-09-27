using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.DTO;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/goal")]
    public class GoalController : ControllerBase
    {
        private readonly IGoalService _goalService;
        private readonly ILogger<GoalController> _logger;

        public GoalController(IGoalService goalService, ILogger<GoalController> logger)
        {
            _goalService = goalService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGoal(int id)
        {
            _logger.LogInformation("Fetching goal with ID: {ProfileId}", id);
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid Profile ID requested: {ProfileId}", id);
                    return BadRequest("Invalid Profile ID");
                }

                var goal = await _goalService.GetGoalByIdAsync(id);
                if (goal == null)
                {
                    _logger.LogWarning("Goal not found for ID: {ProfileId}", id);
                    return NotFound("Goal not found");
                }

                _logger.LogInformation("Successfully retrieved goal ID: {ProfileId}", id);
                return Ok(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goal ID: {ProfileId}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGoal([FromBody] GoalDTO newGoal)
        {
            _logger.LogInformation("Starting CreateGoal for ProfileId: {ProfileId}", newGoal?.ProfileId);
            try
            {
                if (newGoal == null)
                {
                    _logger.LogWarning("Empty goal creation request received");
                    return BadRequest("Goal data cannot be empty");
                }

                _logger.LogDebug("Validation checks for ProfileId: {ProfileId}", newGoal.ProfileId);

                // Parameter validation
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

                if (await _goalService.ExistsByProfileIdAsync(newGoal.ProfileId))
                {
                    _logger.LogWarning("Duplicate goal attempt for ProfileId: {ProfileId}", newGoal.ProfileId);
                    return Conflict("A goal already exists for this profile.");
                }

                if (await _goalService.CreateGoalAsync(newGoal))
                {
                    _logger.LogInformation("Goal created successfully for ProfileId: {ProfileId}", newGoal.ProfileId);
                    return Ok("Goal created successfully");
                }

                _logger.LogError("Goal creation failed for ProfileId: {ProfileId}", newGoal.ProfileId);
                return StatusCode(500, "Creation failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for ProfileId: {ProfileId}", newGoal?.ProfileId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}