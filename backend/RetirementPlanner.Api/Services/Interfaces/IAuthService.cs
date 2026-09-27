using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>Creates the user and profile and signs them in. EmailTaken if the email is registered.</summary>
        Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);

        /// <summary>Signs in with email and password, starting a new refresh-token family.</summary>
        Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exchanges a refresh token for a new access token and a new refresh token (rotation). Presenting a
        /// token that was already rotated or revoked revokes its whole family and returns RefreshTokenReused.
        /// </summary>
        Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default);

        /// <summary>Revokes the refresh token's family. Does nothing for an unknown or missing token.</summary>
        Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    }
}
