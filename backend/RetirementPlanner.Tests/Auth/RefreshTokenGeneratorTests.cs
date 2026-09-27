using RetirementPlanner.Auth;

namespace RetirementPlanner.Tests.Auth
{
    public class RefreshTokenGeneratorTests
    {
        [Fact]
        public void Create_ReturnsRandomTokenAndItsHash()
        {
            var (token, hash) = RefreshTokenGenerator.Create();
            var (other, _) = RefreshTokenGenerator.Create();

            Assert.NotEqual(token, other);
            Assert.Equal(43, token.Length); // 32 random bytes, base64url without padding
            Assert.Equal(RefreshTokenGenerator.Hash(token), hash);
            Assert.Matches("^[0-9a-f]{64}$", hash);
            Assert.DoesNotContain(token, hash);
        }
    }
}
