using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RetirementPlanner.Models;

namespace RetirementPlanner.Auth
{
    public class JwtAccessTokenService : IAccessTokenService
    {
        private readonly JwtOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly SigningCredentials _signingCredentials;
        private readonly JsonWebTokenHandler _handler = new();

        public JwtAccessTokenService(JwtOptions options, TimeProvider timeProvider)
        {
            _options = options;
            _timeProvider = timeProvider;
            _signingCredentials = new SigningCredentials(CreateSigningKey(options), SecurityAlgorithms.HmacSha256);
        }

        public AccessToken CreateAccessToken(AuthenticatedUser user)
        {
            var now = _timeProvider.GetUtcNow();
            var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

            var token = _handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ]),
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = _signingCredentials
            });

            return new AccessToken(token, expiresAt);
        }

        /// <summary>
        /// The parameters the API validates access tokens with. The tests use the same method, so they
        /// check tokens exactly the way the JwtBearer handler does.
        /// </summary>
        public static TokenValidationParameters CreateValidationParameters(JwtOptions options) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(options),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            // The default 5-minute skew would stretch a 15-minute token to 20.
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JwtRegisteredClaimNames.Sub
        };

        private static SymmetricSecurityKey CreateSigningKey(JwtOptions options) =>
            new(Encoding.UTF8.GetBytes(options.SigningKey));
    }
}
