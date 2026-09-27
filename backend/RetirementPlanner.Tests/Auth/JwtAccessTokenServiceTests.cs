using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RetirementPlanner.Auth;
using RetirementPlanner.Models;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Auth
{
    public class JwtAccessTokenServiceTests
    {
        internal static readonly JwtOptions Options = new()
        {
            Issuer = "retirement-planner-api",
            Audience = "retirement-planner-web",
            SigningKey = "unit-test-signing-key-that-is-at-least-32-bytes-long",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        private static readonly AuthenticatedUser User = new() { UserId = 42, Email = "jane@example.com" };

        private static JwtAccessTokenService CreateService(DateTimeOffset now) => new(Options, new FixedTimeProvider(now));

        private static Task<TokenValidationResult> ValidateAsync(string token, JwtOptions? options = null) =>
            new JsonWebTokenHandler().ValidateTokenAsync(token, JwtAccessTokenService.CreateValidationParameters(options ?? Options));

        [Fact]
        public async Task CreateAccessToken_IssuesATokenTheApiAccepts_WithUserClaims()
        {
            var token = CreateService(DateTimeOffset.UtcNow).CreateAccessToken(User);

            var result = await ValidateAsync(token.Token);

            Assert.True(result.IsValid, result.Exception?.Message);
            Assert.Equal("42", result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
            Assert.Equal("jane@example.com", result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)!.Value);
            Assert.False(string.IsNullOrEmpty(result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
        }

        [Fact]
        public void CreateAccessToken_ExpiresFifteenMinutesAfterIssue()
        {
            var now = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

            var token = CreateService(now).CreateAccessToken(User);
            var jwt = new JsonWebToken(token.Token);

            Assert.Equal(now.AddMinutes(15), token.ExpiresAt);
            Assert.Equal(now.AddMinutes(15).UtcDateTime, jwt.ValidTo);
            Assert.Equal(now.UtcDateTime, jwt.IssuedAt);
            Assert.Equal("HS256", jwt.Alg);
        }

        [Fact]
        public async Task ExpiredToken_IsRejected()
        {
            // Issued 20 minutes ago: 15-minute lifetime plus the 30-second skew has passed.
            var token = CreateService(DateTimeOffset.UtcNow.AddMinutes(-20)).CreateAccessToken(User);

            var result = await ValidateAsync(token.Token);

            Assert.False(result.IsValid);
            Assert.IsType<SecurityTokenExpiredException>(result.Exception);
        }

        [Fact]
        public async Task TokenSignedWithAnotherKey_IsRejected()
        {
            var otherKey = new JwtOptions
            {
                Issuer = Options.Issuer, Audience = Options.Audience,
                SigningKey = "a-completely-different-signing-key-of-32-bytes-plus"
            };
            var token = new JwtAccessTokenService(otherKey, TimeProvider.System).CreateAccessToken(User);

            var result = await ValidateAsync(token.Token);

            Assert.False(result.IsValid);
            Assert.IsAssignableFrom<SecurityTokenSignatureKeyNotFoundException>(result.Exception);
        }

        [Fact]
        public async Task TamperedPayload_IsRejected()
        {
            var token = CreateService(DateTimeOffset.UtcNow).CreateAccessToken(User).Token;
            var parts = token.Split('.');
            var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1])).Replace("\"42\"", "\"43\"");
            var tampered = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";

            var result = await ValidateAsync(tampered);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("another-issuer", "retirement-planner-web")]
        [InlineData("retirement-planner-api", "another-audience")]
        public async Task WrongIssuerOrAudience_IsRejected(string issuer, string audience)
        {
            var token = CreateService(DateTimeOffset.UtcNow).CreateAccessToken(User);
            var expecting = new JwtOptions { Issuer = issuer, Audience = audience, SigningKey = Options.SigningKey };

            var result = await ValidateAsync(token.Token, expecting);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task UnsignedToken_IsRejected()
        {
            var unsigned = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Options.Issuer,
                Audience = Options.Audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = "42" }
            });

            var result = await ValidateAsync(unsigned);

            Assert.False(result.IsValid);
        }
    }
}
