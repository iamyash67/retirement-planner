using RetirementPlanner.Auth;

namespace RetirementPlanner.Models
{
    public enum AuthStatus
    {
        Success,
        InvalidCredentials,
        EmailTaken,
        InvalidRefreshToken,
        RefreshTokenReused
    }

    /// <summary>A signed-in session: the user, a short-lived access token and the raw refresh token for the cookie.</summary>
    public record AuthSession(
        AuthenticatedUser User,
        AccessToken AccessToken,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);

    /// <summary>The outcome of an auth operation. Session is set when Status is Success.</summary>
    public record AuthResult(AuthStatus Status, AuthSession? Session = null);
}
