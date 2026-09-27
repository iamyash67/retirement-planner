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
        public async Task<IActionResult> Login([FromBody] Login login)
        {
            _logger.LogInformation("Login attempt for username: {Username}", login?.UserName);
            try
            {
                if (login == null)
                {
                    _logger.LogWarning("Empty login request received");
                    return BadRequest("Login credentials are required");
                }

                if (string.IsNullOrWhiteSpace(login.UserName))
                {
                    _logger.LogWarning("Missing username in login request");
                    return BadRequest("Username is required");
                }

                if (string.IsNullOrWhiteSpace(login.Password))
                {
                    _logger.LogWarning("Missing password for username: {Username}", login.UserName);
                    return BadRequest("Password is required");
                }

                var profile = await _userService.AuthenticateAsync(login.UserName, login.Password);
                if (profile == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", login.UserName);
                    return Unauthorized("Invalid username or password");
                }

                _logger.LogInformation("Successful login for username: {Username}", login.UserName);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication error for username: {Username}", login?.UserName);
                return StatusCode(500, "Internal server error during authentication");
            }
        }
    }
}