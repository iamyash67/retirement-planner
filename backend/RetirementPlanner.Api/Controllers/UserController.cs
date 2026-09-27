using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login login, CancellationToken cancellationToken)
        {
            if (login == null)
                return BadRequest("Login credentials are required");

            if (string.IsNullOrWhiteSpace(login.UserName))
                return BadRequest("Username is required");

            if (string.IsNullOrWhiteSpace(login.Password))
                return BadRequest("Password is required");

            var profile = await _userService.AuthenticateAsync(login.UserName, login.Password, cancellationToken);
            if (profile == null)
                return Unauthorized("Invalid username or password");

            _logger.LogInformation("Successful login for user {UserId}", profile.ProfileId);
            return Ok(profile);
        }
    }
}
