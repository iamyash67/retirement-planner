using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RetirementPlanner.Auth;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request.ToCommand(), cancellationToken);
            return result.Status switch
            {
                AuthStatus.Success => StatusCode(StatusCodes.Status201Created, StartSession(result.Session!)),
                AuthStatus.EmailTaken => Conflict("Email is already registered"),
                var status => throw new InvalidOperationException($"Unexpected register status {status}.")
            };
        }

        [HttpPost("login")]
        [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            return result.Status == AuthStatus.Success
                ? Ok(StartSession(result.Session!))
                : Unauthorized("Invalid email or password");
        }

        /// <summary>Rotates the refresh token from the cookie and returns a new access token.</summary>
        [HttpPost("refresh")]
        [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshAsync(RefreshTokenCookie.Read(Request), cancellationToken);
            if (result.Status == AuthStatus.Success)
                return Ok(StartSession(result.Session!));

            // Reuse and invalid tokens get the same answer, so the response reveals nothing about the token.
            RefreshTokenCookie.Delete(Response);
            return Unauthorized("Invalid or expired refresh token");
        }

        /// <summary>Ends the session: revokes the refresh token family and clears the cookie.</summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(RefreshTokenCookie.Read(Request), cancellationToken);
            RefreshTokenCookie.Delete(Response);
            return NoContent();
        }

        private AuthResponse StartSession(AuthSession session)
        {
            RefreshTokenCookie.Append(Response, session.RefreshToken, session.RefreshTokenExpiresAt);
            return session.ToResponse();
        }
    }
}
