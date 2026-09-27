using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>
    /// User B must not be able to read or modify user A's goals, and the user id can only come from the
    /// access token. Another user's goal is indistinguishable from a missing one (404).
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public sealed class AuthorizationTests(MySqlFixture db) : IAsyncLifetime
    {
        private readonly ApiFactory _factory = new(db);
        private HttpClient _alice = null!;
        private HttpClient _bob = null!;
        private TestUser _aliceUser = null!;
        private TestUser _bobUser = null!;
        private int _aliceGoalId;

        public async Task InitializeAsync()
        {
            _aliceUser = await _factory.RegisterAsync();
            _bobUser = await _factory.RegisterAsync();
            _alice = _factory.CreateApiClient(_aliceUser.AccessToken);
            _bob = _factory.CreateApiClient(_bobUser.AccessToken);

            var created = await _alice.PostAsJsonAsync("/api/goals",
                new { currentAge = 30, retirementAge = 60, targetSavings = 10_000, currentSavings = 1_000 });
            _aliceGoalId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        }

        public async Task DisposeAsync() => await _factory.DisposeAsync();

        [Fact]
        public async Task OtherUser_CannotReadTheGoal()
        {
            await HttpAssert.PlainAsync(await _bob.GetAsync($"/api/goals/{_aliceGoalId}"), HttpStatusCode.NotFound, "Goal not found");
        }

        [Fact]
        public async Task OtherUser_CannotReadTheProgress()
        {
            await HttpAssert.PlainAsync(await _bob.GetAsync($"/api/goals/{_aliceGoalId}/progress"), HttpStatusCode.NotFound, "Goal not found");
        }

        [Fact]
        public async Task OtherUser_DoesNotSeeTheGoalInTheirList()
        {
            var bobGoals = await _bob.GetFromJsonAsync<JsonElement>("/api/goals");
            var aliceGoals = await _alice.GetFromJsonAsync<JsonElement>("/api/goals");

            Assert.Equal(0, bobGoals.GetArrayLength());
            Assert.Equal([_aliceGoalId], aliceGoals.EnumerateArray().Select(g => g.GetProperty("id").GetInt32()));
        }

        [Fact]
        public async Task OtherUser_CannotAddAContribution_AndNothingIsWritten()
        {
            var response = await _bob.PostAsJsonAsync($"/api/goals/{_aliceGoalId}/contributions",
                new { year = 2026, month = 3, amount = 500 });

            await HttpAssert.PlainAsync(response, HttpStatusCode.NotFound, "Goal not found");
            Assert.Equal(0, await db.ScalarAsync<long>("SELECT COUNT(*) FROM Contributions WHERE GoalId = @id", new { id = _aliceGoalId }));
            var goal = await _alice.GetFromJsonAsync<JsonElement>($"/api/goals/{_aliceGoalId}");
            Assert.Equal(1_000m, goal.GetProperty("currentSavings").GetDecimal());
        }

        [Fact]
        public async Task UserIdInTheBody_IsIgnored_TheGoalBelongsToTheCaller()
        {
            // Bob tries to create a goal "for" Alice by putting her id in the body.
            var response = await _bob.PostAsJsonAsync("/api/goals", new
            {
                userId = _aliceUser.Id, profileId = _aliceUser.Id,
                currentAge = 40, retirementAge = 65, targetSavings = 50_000, currentSavings = 0
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var goalId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            Assert.Equal(_bobUser.Id, await db.ScalarAsync<int>("SELECT UserId FROM Goals WHERE Id = @goalId", new { goalId }));
            Assert.Equal(1, (await _alice.GetFromJsonAsync<JsonElement>("/api/goals")).GetArrayLength());
        }

        [Theory]
        [InlineData("GET", "/api/goals")]
        [InlineData("POST", "/api/goals")]
        [InlineData("GET", "/api/goals/{id}")]
        [InlineData("POST", "/api/goals/{id}/contributions")]
        [InlineData("GET", "/api/goals/{id}/progress")]
        public async Task EveryGoalEndpoint_RequiresAnAccessToken(string method, string route)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), route.Replace("{id}", _aliceGoalId.ToString()));
            if (method == "POST")
                request.Content = JsonContent.Create(new { year = 2026, month = 4, amount = 1, currentAge = 30, retirementAge = 60, targetSavings = 10 });

            var response = await _factory.CreateApiClient().SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(0, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM Contributions WHERE GoalId = @id AND `Month` = 4", new { id = _aliceGoalId }));
        }

        [Fact]
        public async Task OldRoutesThatTookAProfileId_AreGone()
        {
            Assert.Equal(HttpStatusCode.NotFound, (await _bob.GetAsync($"/api/goal/{_aliceUser.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _bob.GetAsync($"/api/financial/progress/{_aliceGoalId}")).StatusCode);
        }
    }
}
