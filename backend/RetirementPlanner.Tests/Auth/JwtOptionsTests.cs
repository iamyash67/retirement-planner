using Microsoft.Extensions.Configuration;
using RetirementPlanner.Auth;

namespace RetirementPlanner.Tests.Auth
{
    public class JwtOptionsTests
    {
        private static IConfiguration Config(string? signingKey) => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:SigningKey"] = signingKey
            })
            .Build();

        [Fact]
        public void Load_WithStrongKey_ReturnsOptionsWithDefaultLifetimes()
        {
            var options = JwtOptions.Load(Config(new string('k', 32)));

            Assert.Equal(15, options.AccessTokenMinutes);
            Assert.Equal(7, options.RefreshTokenDays);
            Assert.Equal(10, options.RefreshTokenReuseGraceSeconds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Load_WithoutKey_Throws(string? key)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => JwtOptions.Load(Config(key)));

            Assert.Contains("Jwt:SigningKey is not configured", ex.Message);
        }

        [Fact]
        public void Load_WithNegativeReuseGracePeriod_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddConfiguration(Config(new string('k', 32)))
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:RefreshTokenReuseGraceSeconds"] = "-1" })
                .Build();

            Assert.Throws<InvalidOperationException>(() => JwtOptions.Load(config));
        }

        [Fact]
        public void Load_WithShortKey_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => JwtOptions.Load(Config(new string('k', 31))));

            Assert.Contains("at least 32 bytes", ex.Message);
        }
    }
}
