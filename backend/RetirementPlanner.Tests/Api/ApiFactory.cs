using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RetirementPlanner.Auth;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>The real API (Program.cs) against the Testcontainers database.</summary>
    public sealed class ApiFactory(
        MySqlFixture db,
        int authPermitLimit = 10_000,
        string signingKey = ApiFactory.TestSigningKey,
        int? reuseGraceSeconds = null)
        : WebApplicationFactory<Program>
    {
        public const string TestSigningKey = "api-test-signing-key-that-is-at-least-32-bytes";
        public const string Password = "correct-horse";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", db.ConnectionString);
            builder.UseSetting("Jwt:SigningKey", signingKey);
            builder.UseSetting("RateLimiting:Auth:PermitLimit", authPermitLimit.ToString());
            if (reuseGraceSeconds != null)
                builder.UseSetting("Jwt:RefreshTokenReuseGraceSeconds", reuseGraceSeconds.Value.ToString());
        }

        /// <summary>
        /// An HTTPS client (the refresh cookie is Secure) that doesn't store cookies, so each test sends
        /// exactly the cookie it means to.
        /// </summary>
        public HttpClient CreateApiClient(string? accessToken = null)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
                AllowAutoRedirect = false
            });
            if (accessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        /// <summary>Registers a new user through the API and returns their tokens.</summary>
        public async Task<TestUser> RegisterAsync()
        {
            var email = MySqlFixture.UniqueEmail();
            var response = await CreateApiClient().PostAsJsonAsync("/api/auth/register", new
            {
                email, password = Password, firstName = "Api", lastName = "Tester", dateOfBirth = "1990-01-01"
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new TestUser(
                body.GetProperty("user").GetProperty("id").GetInt32(),
                email,
                body.GetProperty("accessToken").GetString()!,
                RefreshCookie.From(response)!);
        }
    }

    public record TestUser(int Id, string Email, string AccessToken, string RefreshToken);

    public static class RefreshCookie
    {
        /// <summary>The rp_refresh value from a response's Set-Cookie header, or null if it wasn't set.</summary>
        public static string? From(HttpResponseMessage response) => Header(response)?.Split(';')[0][(RefreshTokenCookie.Name.Length + 1)..];

        /// <summary>The full rp_refresh Set-Cookie header, attributes included.</summary>
        public static string? Header(HttpResponseMessage response) =>
            response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.FirstOrDefault(v => v.StartsWith(RefreshTokenCookie.Name + "=", StringComparison.Ordinal))
                : null;

        public static HttpRequestMessage Post(string url, string? refreshToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (refreshToken != null)
                request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
            return request;
        }
    }
}
