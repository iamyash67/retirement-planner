using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>Login, register and refresh share one fixed-window limit per client IP.</summary>
    [Collection(MySqlCollection.Name)]
    public sealed class RateLimitTests(MySqlFixture db)
    {
        private const int Limit = 3;

        [Fact]
        public async Task Login_BeyondTheLimit_Returns429WithRetryAfter()
        {
            await using var factory = new ApiFactory(db, authPermitLimit: Limit);
            var client = factory.CreateApiClient();
            var credentials = new { email = MySqlFixture.UniqueEmail(), password = "wrong-password" };

            for (var i = 0; i < Limit; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);

            var limited = await client.PostAsJsonAsync("/api/auth/login", credentials);

            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero);
            Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
            Assert.Equal(429, (await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task RegisterAndRefresh_AreLimitedToo_AndShareTheBudget()
        {
            await using var factory = new ApiFactory(db, authPermitLimit: Limit);
            var client = factory.CreateApiClient();

            // One register, one refresh and one login use up the window...
            await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid" });
            await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", "x"));
            await client.PostAsJsonAsync("/api/auth/login", new { email = "a@b.c", password = "x" });

            // ...so each of them is now rejected.
            Assert.Equal(HttpStatusCode.TooManyRequests,
                (await client.PostAsJsonAsync("/api/auth/register", new { email = "invalid" })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests,
                (await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", "x"))).StatusCode);
        }

        [Fact]
        public async Task OtherEndpoints_AreNotRateLimited()
        {
            await using var factory = new ApiFactory(db, authPermitLimit: Limit);
            var client = factory.CreateApiClient();

            for (var i = 0; i < Limit * 3; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/goals")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.SendAsync(RefreshCookie.Post("/api/auth/logout", null))).StatusCode);
        }
    }
}
