using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    [Collection(MySqlCollection.Name)]
    public sealed class StartupTests(MySqlFixture db)
    {
        [Theory]
        [InlineData("", "Jwt:SigningKey is not configured")]
        [InlineData("too-short", "must be at least 32 bytes")]
        public async Task Startup_WithoutAUsableSigningKey_Fails(string signingKey, string expectedMessage)
        {
            await using var factory = new ApiFactory(db, signingKey: signingKey);

            // Program.cs throws while building the host, so creating a client fails with our message.
            var ex = Assert.ThrowsAny<Exception>(() => factory.CreateApiClient());

            Assert.Contains(expectedMessage, ex.ToString());
        }
    }
}
