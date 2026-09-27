using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    [Collection(MySqlCollection.Name)]
    public sealed class AuthApiTests(MySqlFixture db) : IAsyncLifetime
    {
        private readonly ApiFactory _factory = new(db);

        public Task InitializeAsync() => Task.CompletedTask;
        public async Task DisposeAsync() => await _factory.DisposeAsync();

        [Fact]
        public async Task Register_Returns201_WithAccessTokenAndUser_AndSetsAHardenedRefreshCookie()
        {
            var email = MySqlFixture.UniqueEmail();

            var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", new
            {
                email, password = ApiFactory.Password, firstName = "Jane", lastName = "Doe", dateOfBirth = "1990-06-15", gender = "Female"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(["accessToken", "expiresAt", "user"], HttpAssert.PropertyNames(body));
            Assert.Equal(["id", "email", "firstName", "lastName", "age", "gender"], HttpAssert.PropertyNames(body.GetProperty("user")));
            Assert.Equal(email, body.GetProperty("user").GetProperty("email").GetString());

            var jwt = new JsonWebToken(body.GetProperty("accessToken").GetString());
            Assert.Equal(body.GetProperty("user").GetProperty("id").GetInt32().ToString(), jwt.Subject);
            Assert.InRange(jwt.ValidTo - jwt.IssuedAt, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));

            var cookie = RefreshCookie.Header(response)!.ToLowerInvariant();
            Assert.Contains("httponly", cookie);
            Assert.Contains("secure", cookie);
            Assert.Contains("samesite=strict", cookie);
            Assert.Contains("path=/api/auth", cookie);
            Assert.Contains("expires=", cookie);
            // The refresh token is only in the cookie, never in the body.
            Assert.DoesNotContain(RefreshCookie.From(response)!, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Register_WithTakenEmail_Returns409PlainString()
        {
            var user = await _factory.RegisterAsync();

            var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", new
            {
                email = user.Email, password = ApiFactory.Password, firstName = "A", lastName = "B", dateOfBirth = "1990-01-01"
            });

            await HttpAssert.PlainAsync(response, HttpStatusCode.Conflict, "Email is already registered");
        }

        [Fact]
        public async Task Register_WithInvalidFields_Returns400FieldErrors()
        {
            var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register",
                new { email = "nope", password = "short", firstName = "", lastName = "Doe" });

            var errors = await HttpAssert.ValidationProblemAsync(response);
            Assert.Equal(["Email is not a valid email address"], errors["email"]);
            Assert.Equal(["Password must be at least 8 characters"], errors["password"]);
            Assert.Equal(["First name is required"], errors["firstName"]);
            Assert.Equal(["Date of birth is required"], errors["dateOfBirth"]);
            Assert.Null(RefreshCookie.Header(response));
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200AndCookie()
        {
            var user = await _factory.RegisterAsync();

            var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/login",
                new { email = user.Email, password = ApiFactory.Password });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(user.Id, body.GetProperty("user").GetProperty("id").GetInt32());
            Assert.NotNull(RefreshCookie.From(response));
        }

        [Fact]
        public async Task Login_WithWrongPasswordOrUnknownEmail_Returns401WithoutCookie()
        {
            var user = await _factory.RegisterAsync();
            var client = _factory.CreateApiClient();

            var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "wrong-password" });
            var unknown = await client.PostAsJsonAsync("/api/auth/login", new { email = MySqlFixture.UniqueEmail(), password = "x" });

            await HttpAssert.PlainAsync(wrong, HttpStatusCode.Unauthorized, "Invalid email or password");
            await HttpAssert.PlainAsync(unknown, HttpStatusCode.Unauthorized, "Invalid email or password");
            Assert.Null(RefreshCookie.Header(wrong));
        }

        [Fact]
        public async Task Login_WithMissingFields_Returns400()
        {
            var errors = await HttpAssert.ValidationProblemAsync(
                await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/login", new { }));

            Assert.Equal(["Email is required"], errors["email"]);
            Assert.Equal(["Password is required"], errors["password"]);
        }

        [Fact]
        public async Task Refresh_RotatesTheCookie_AndReusingTheOldOneRevokesTheFamily()
        {
            // Grace period 0, so the immediate replay below counts as reuse (the default tolerates 10 s).
            await using var factory = new ApiFactory(db, reuseGraceSeconds: 0);
            var user = await factory.RegisterAsync();
            var client = factory.CreateApiClient();

            var first = await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", user.RefreshToken));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var rotated = RefreshCookie.From(first)!;
            Assert.NotEqual(user.RefreshToken, rotated);
            var body = await first.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(user.Id, body.GetProperty("user").GetProperty("id").GetInt32());

            // The new access token works.
            var withNewToken = factory.CreateApiClient(body.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await withNewToken.GetAsync("/api/goals")).StatusCode);

            // Replaying the rotated-out cookie is reuse: 401, and the cookie is cleared...
            var replay = await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", user.RefreshToken));
            await HttpAssert.PlainAsync(replay, HttpStatusCode.Unauthorized, "Invalid or expired refresh token");
            Assert.Contains("expires=thu, 01 jan 1970", RefreshCookie.Header(replay)!.ToLowerInvariant());

            // ...and the newest token of that family no longer works either.
            var afterReuse = await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", rotated));
            Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
        }

        [Fact]
        public async Task TwoConcurrentRefreshesWithTheSameCookie_OneWins_AndTheFamilyIsNotRevoked()
        {
            // Two tabs share the cookie and refresh at the same moment.
            var user = await _factory.RegisterAsync();
            var responses = await Task.WhenAll(
                _factory.CreateApiClient().SendAsync(RefreshCookie.Post("/api/auth/refresh", user.RefreshToken)),
                _factory.CreateApiClient().SendAsync(RefreshCookie.Post("/api/auth/refresh", user.RefreshToken)));

            var winner = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            var loser = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Unauthorized);
            Assert.Equal("Invalid or expired refresh token", await loser.Content.ReadAsStringAsync());

            // The family is intact: the rotated token is the one live token, and the session carries on.
            Assert.Equal(1, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM RefreshTokens WHERE RevokedAt IS NULL AND FamilyId = (SELECT FamilyId FROM RefreshTokens WHERE TokenHash = @hash)",
                new { hash = RetirementPlanner.Auth.RefreshTokenGenerator.Hash(user.RefreshToken) }));
            var next = await _factory.CreateApiClient().SendAsync(
                RefreshCookie.Post("/api/auth/refresh", RefreshCookie.From(winner)));
            Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not-a-real-token")]
        public async Task Refresh_WithoutAValidCookie_Returns401(string? cookie)
        {
            var response = await _factory.CreateApiClient().SendAsync(RefreshCookie.Post("/api/auth/refresh", cookie));

            await HttpAssert.PlainAsync(response, HttpStatusCode.Unauthorized, "Invalid or expired refresh token");
        }

        [Fact]
        public async Task Logout_Returns204_ClearsTheCookie_AndRevokesTheRefreshToken()
        {
            var user = await _factory.RegisterAsync();
            var client = _factory.CreateApiClient();

            var logout = await client.SendAsync(RefreshCookie.Post("/api/auth/logout", user.RefreshToken));

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Contains("expires=thu, 01 jan 1970", RefreshCookie.Header(logout)!.ToLowerInvariant());
            var refresh = await client.SendAsync(RefreshCookie.Post("/api/auth/refresh", user.RefreshToken));
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        }

        [Fact]
        public async Task Logout_WithoutCookie_StillReturns204()
        {
            var response = await _factory.CreateApiClient().SendAsync(RefreshCookie.Post("/api/auth/logout", null));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("garbage")]
        [InlineData("eyJhbGciOiJub25lIn0.eyJzdWIiOiIxIn0.")] // alg=none
        public async Task ProtectedEndpoint_WithoutAValidAccessToken_Returns401(string? token)
        {
            var response = await _factory.CreateApiClient(token).GetAsync("/api/goals");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AccessTokenSignedWithAnotherKey_Returns401()
        {
            await using var otherKeyFactory = new ApiFactory(db, signingKey: "a-different-signing-key-with-32-bytes-or-more");
            var foreign = await otherKeyFactory.RegisterAsync();

            var response = await _factory.CreateApiClient(foreign.AccessToken).GetAsync("/api/goals");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
