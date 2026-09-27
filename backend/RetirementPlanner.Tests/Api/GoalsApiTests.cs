using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>The goals endpoints for a signed-in user: shapes, status codes, validation and messages.</summary>
    [Collection(MySqlCollection.Name)]
    public sealed class GoalsApiTests(MySqlFixture db) : IAsyncLifetime
    {
        private static readonly string[] GoalFields =
            ["id", "name", "currentAge", "retirementAge", "targetSavings", "monthlyContribution", "currentSavings"];

        private readonly ApiFactory _factory = new(db);

        public Task InitializeAsync() => Task.CompletedTask;
        public async Task DisposeAsync() => await _factory.DisposeAsync();

        private async Task<HttpClient> SignedInClientAsync() =>
            _factory.CreateApiClient((await _factory.RegisterAsync()).AccessToken);

        private static readonly object ValidGoal =
            new { currentAge = 30, retirementAge = 60, targetSavings = 1_000_000, currentSavings = 100_000 };

        [Fact]
        public async Task CreateGoal_Returns201WithLocation_ThenGetAndListReturnIt_ThenSecondIs409()
        {
            var client = await SignedInClientAsync();

            var created = await client.PostAsJsonAsync("/api/goals", ValidGoal);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var goal = await created.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(GoalFields, HttpAssert.PropertyNames(goal));
            var id = goal.GetProperty("id").GetInt32();
            Assert.Equal($"/api/goals/{id}", created.Headers.Location!.AbsolutePath);
            Assert.Equal(2500m, goal.GetProperty("monthlyContribution").GetDecimal());
            Assert.Equal("Retirement", goal.GetProperty("name").GetString());

            var fetched = await client.GetFromJsonAsync<JsonElement>($"/api/goals/{id}");
            Assert.Equal(id, fetched.GetProperty("id").GetInt32());

            var list = await client.GetFromJsonAsync<JsonElement>("/api/goals");
            Assert.Equal([id], list.EnumerateArray().Select(g => g.GetProperty("id").GetInt32()));

            await HttpAssert.PlainAsync(await client.PostAsJsonAsync("/api/goals", ValidGoal),
                HttpStatusCode.Conflict, "A goal already exists for this user.");
        }

        [Fact]
        public async Task ListGoals_ForNewUser_ReturnsEmptyArray()
        {
            var client = await SignedInClientAsync();

            var list = await client.GetFromJsonAsync<JsonElement>("/api/goals");

            Assert.Equal(JsonValueKind.Array, list.ValueKind);
            Assert.Equal(0, list.GetArrayLength());
        }

        [Fact]
        public async Task CreateGoal_WithInvalidFields_Returns400WithEveryFieldError()
        {
            var client = await SignedInClientAsync();

            var errors = await HttpAssert.ValidationProblemAsync(await client.PostAsJsonAsync("/api/goals",
                new { currentAge = 60, retirementAge = 50, targetSavings = 100, currentSavings = 200, inflationRate = 2 }));

            Assert.Equal(["Retirement age must be greater than current age"], errors["retirementAge"]);
            Assert.Equal(["You have enough savings to reach your goal"], errors["currentSavings"]);
            Assert.Equal(["Inflation rate must be between -0.5 and 1"], errors["inflationRate"]);
        }

        [Fact]
        public async Task GoalRoutes_WithNonPositiveId_Return400_AndUnknownIdReturns404()
        {
            var client = await SignedInClientAsync();

            Assert.Equal(["Invalid Goal ID"], (await HttpAssert.ValidationProblemAsync(await client.GetAsync("/api/goals/0")))["id"]);
            Assert.Equal(["Invalid Goal ID"], (await HttpAssert.ValidationProblemAsync(await client.GetAsync("/api/goals/-1/progress")))["id"]);
            await HttpAssert.PlainAsync(await client.GetAsync($"/api/goals/{int.MaxValue}"), HttpStatusCode.NotFound, "Goal not found");
            await HttpAssert.PlainAsync(await client.GetAsync($"/api/goals/{int.MaxValue}/progress"), HttpStatusCode.NotFound, "Goal not found");
            await HttpAssert.PlainAsync(
                await client.PostAsJsonAsync($"/api/goals/{int.MaxValue}/contributions", new { year = 2026, month = 1, amount = 10 }),
                HttpStatusCode.NotFound, "Goal not found");
        }

        [Fact]
        public async Task Contributions_FullFlow_KeepsStatusCodesAndMessages()
        {
            var client = await SignedInClientAsync();
            var created = await client.PostAsJsonAsync("/api/goals",
                new { currentAge = 30, retirementAge = 60, targetSavings = 10_000, currentSavings = 1_000 });
            var goalId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            var url = $"/api/goals/{goalId}/contributions";

            var invalid = await HttpAssert.ValidationProblemAsync(
                await client.PostAsJsonAsync(url, new { year = 1979, month = 13, amount = 0 }));
            Assert.Equal(["Invalid Year"], invalid["year"]);
            Assert.Equal(["Month must be between 1-12"], invalid["month"]);
            Assert.Equal(["Amount must be positive"], invalid["amount"]);

            var tooMuch = await HttpAssert.ValidationProblemAsync(
                await client.PostAsJsonAsync(url, new { year = 2026, month = 1, amount = 10_001 }));
            Assert.Equal(["Amount cannot exceed target savings"], tooMuch["amount"]);

            var contribution = new { year = 2026, month = 1, amount = 500 };
            var recorded = await client.PostAsJsonAsync(url, contribution);
            Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
            var body = await recorded.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(["id", "goalId", "year", "month", "amount", "recordedAt"], HttpAssert.PropertyNames(body));
            Assert.Equal(goalId, body.GetProperty("goalId").GetInt32());
            Assert.Equal(500m, body.GetProperty("amount").GetDecimal());

            await HttpAssert.PlainAsync(await client.PostAsJsonAsync(url, contribution),
                HttpStatusCode.Conflict, "A contribution is already recorded for this month");

            var goal = await client.GetFromJsonAsync<JsonElement>($"/api/goals/{goalId}");
            Assert.Equal(1_500m, goal.GetProperty("currentSavings").GetDecimal());

            var progress = await client.GetFromJsonAsync<JsonElement>($"/api/goals/{goalId}/progress");
            Assert.Equal(["goalId", "progress"], HttpAssert.PropertyNames(progress));
            Assert.Equal("15.00%", progress.GetProperty("progress").GetString());
        }
    }
}
