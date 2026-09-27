using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RetirementPlanner.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// The current user's id, from the access token's "sub" claim. This is the only source of the user
        /// id for authenticated endpoints; routes and bodies never carry it.
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(subject, out var userId)
                ? userId
                : throw new InvalidOperationException("The authenticated principal has no numeric 'sub' claim.");
        }
    }
}
