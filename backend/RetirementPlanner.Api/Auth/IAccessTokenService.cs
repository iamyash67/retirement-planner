using RetirementPlanner.Models;

namespace RetirementPlanner.Auth
{
    public interface IAccessTokenService
    {
        /// <summary>Creates a signed JWT for the user, valid for Jwt:AccessTokenMinutes.</summary>
        AccessToken CreateAccessToken(AuthenticatedUser user);
    }

    public record AccessToken(string Token, DateTimeOffset ExpiresAt);
}
