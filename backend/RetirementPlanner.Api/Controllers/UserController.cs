using Microsoft.AspNetCore.Mvc;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Mapping;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("login")]
        [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var user = await _userService.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
            return user == null
                ? Unauthorized("Invalid username or password")
                : Ok(user.ToLoginResponse());
        }
    }
}
