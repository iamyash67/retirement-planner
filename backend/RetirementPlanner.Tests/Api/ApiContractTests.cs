using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RetirementPlanner.Tests.Integration;

namespace RetirementPlanner.Tests.Api
{
    /// <summary>
    /// End-to-end HTTP tests: validation failures are 400 ValidationProblemDetails with camelCase field keys,
    /// 404 and 409 keep their plain-string bodies, and successful responses have the documented shape.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public sealed class ApiContractTests : IAsyncLifetime
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        public ApiContractTests(MySqlFixture db)
        {
            _factory = new ApiFactory(db);
            _client = _factory.CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _factory.DisposeAsync();

        [Fact]
        public async Task Login_WithEmptyFields_Returns400WithFieldErrors()
        {
            var response = await _client.PostAsJsonAsync("/api/user/login", new { userName = "", password = "" });

            var errors = await AssertValidationProblemAsync(response);
            Assert.Equal(["Username is required"], errors["userName"]);
            Assert.Equal(["Password is required"], errors["password"]);
        }

        [Fact]
        public async Task Login_WithMissingFields_Returns400FromFluentValidation()
        {
            var response = await _client.PostAsJsonAsync("/api/user/login", new { });

            var errors = await AssertValidationProblemAsync(response);
            Assert.Equal(["Username is required"], errors["userName"]);
            Assert.Equal(["Password is required"], errors["password"]);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsProfileWithoutPasswordFields()
        {
            var (userId, email) = await _factory.CreateUserAsync("pass123");

            var response = await _client.PostAsJsonAsync("/api/user/login", new { userName = email, password = "pass123" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(["profileId", "firstName", "lastName", "age", "gender", "userName"],
                body.EnumerateObject().Select(p => p.Name).ToArray());
            Assert.Equal(userId, body.GetProperty("profileId").GetInt32());
            Assert.Equal(email, body.GetProperty("userName").GetString());
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401PlainString()
        {
            var (_, email) = await _factory.CreateUserAsync("pass123");

            var response = await _client.PostAsJsonAsync("/api/user/login", new { userName = email, password = "nope" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("Invalid username or password", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreateGoal_WithInvalidFields_Returns400WithEveryFieldError()
        {
            var response = await _client.PostAsJsonAsync("/api/goal",
                new { profileId = 0, currentAge = 60, retirementAge = 50, targetSavings = 100, currentSavings = 200, inflationRate = 2 });

            var errors = await AssertValidationProblemAsync(response);
            Assert.Equal(["Invalid Profile ID"], errors["profileId"]);
            Assert.Equal(["Retirement age must be greater than current age"], errors["retirementAge"]);
            Assert.Equal(["You have enough savings to reach your goal"], errors["currentSavings"]);
            Assert.Equal(["Inflation rate must be between -0.5 and 1"], errors["inflationRate"]);
        }

        [Fact]
        public async Task CreateGoal_Returns201WithGoal_ThenGetReturnsIt_ThenDuplicateIs409PlainString()
        {
            var (userId, _) = await _factory.CreateUserAsync();
            var request = new { profileId = userId, currentAge = 30, retirementAge = 60, targetSavings = 1_000_000, currentSavings = 100_000 };

            var created = await _client.PostAsJsonAsync("/api/goal", request);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal($"/api/goal/{userId}", created.Headers.Location!.AbsolutePath);
            var goal = await created.Content.ReadFromJsonAsync<JsonElement>();
            AssertGoalShape(goal);
            Assert.Equal(userId, goal.GetProperty("profileId").GetInt32());
            Assert.Equal(2500m, goal.GetProperty("monthlyContribution").GetDecimal());

            var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/goal/{userId}");
            Assert.Equal(goal.GetProperty("goalId").GetInt32(), fetched.GetProperty("goalId").GetInt32());

            var duplicate = await _client.PostAsJsonAsync("/api/goal", request);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("A goal already exists for this profile.", await duplicate.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreateGoal_ForUnknownProfile_Returns404PlainString()
        {
            var response = await _client.PostAsJsonAsync("/api/goal",
                new { profileId = int.MaxValue, currentAge = 30, retirementAge = 60, targetSavings = 1000, currentSavings = 0 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Profile not found", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task GetGoal_WithNonPositiveId_Returns400FieldError()
        {

            var errors = await AssertValidationProblemAsync(await _client.GetAsync("/api/goal/0"));

            Assert.Equal(["Invalid Profile ID"], errors["profileId"]);
        }

        [Fact]
        public async Task GetGoal_WhenUserHasNoGoal_Returns404PlainString()
        {
            var (userId, _) = await _factory.CreateUserAsync();

            var response = await _client.GetAsync($"/api/goal/{userId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Goal not found", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AddInvestment_FullFlow_KeepsStatusCodesAndMessages()
        {
            var goalId = await CreateGoalAsync(targetSavings: 10_000);

            var invalid = await _client.PostAsJsonAsync("/api/financial/Add-Investment",
                new { goalId, year = 1979, month = 13, monthlyInvestment = 0 });
            var errors = await AssertValidationProblemAsync(invalid);
            Assert.Equal(["Invalid Year"], errors["year"]);
            Assert.Equal(["Month must be between 1-12"], errors["month"]);
            Assert.Equal(["Monthly investment must be positive"], errors["monthlyInvestment"]);

            var tooMuch = await _client.PostAsJsonAsync("/api/financial/Add-Investment",
                new { goalId, year = 2026, month = 1, monthlyInvestment = 10_001 });
            var tooMuchErrors = await AssertValidationProblemAsync(tooMuch);
            Assert.Equal(["Monthly investment cannot exceed target savings"], tooMuchErrors["monthlyInvestment"]);

            var investment = new { goalId, year = 2026, month = 1, monthlyInvestment = 500 };
            var recorded = await _client.PostAsJsonAsync("/api/financial/Add-Investment", investment);
            Assert.Equal(HttpStatusCode.OK, recorded.StatusCode);
            var goal = await recorded.Content.ReadFromJsonAsync<JsonElement>();
            AssertGoalShape(goal);
            Assert.Equal(1_500m, goal.GetProperty("currentSavings").GetDecimal());

            var repeated = await _client.PostAsJsonAsync("/api/financial/Add-Investment", investment);
            Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
            Assert.Equal("Investment already recorded", await repeated.Content.ReadAsStringAsync());

            var progress = await _client.GetFromJsonAsync<JsonElement>($"/api/financial/progress/{goalId}");
            Assert.Equal(goalId, progress.GetProperty("goalId").GetInt32());
            Assert.Equal("15.00%", progress.GetProperty("progress").GetString());
        }

        [Fact]
        public async Task AddInvestment_ForUnknownGoal_Returns404PlainString()
        {
            var response = await _client.PostAsJsonAsync("/api/financial/Add-Investment",
                new { goalId = int.MaxValue, year = 2026, month = 1, monthlyInvestment = 10 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("Goal not found", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Progress_WithNonPositiveId_Returns400FieldError_AndUnknownGoalIs404()
        {
            var errors = await AssertValidationProblemAsync(await _client.GetAsync("/api/financial/progress/0"));
            Assert.Equal(["Invalid Goal ID"], errors["goalId"]);

            var missing = await _client.GetAsync($"/api/financial/progress/{int.MaxValue}");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal("Goal not found or TargetSavings is zero", await missing.Content.ReadAsStringAsync());
        }

        private async Task<int> CreateGoalAsync(decimal targetSavings)
        {
            var (userId, _) = await _factory.CreateUserAsync();
            var response = await _client.PostAsJsonAsync("/api/goal",
                new { profileId = userId, currentAge = 30, retirementAge = 60, targetSavings, currentSavings = 1_000 });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("goalId").GetInt32();
        }

        private static void AssertGoalShape(JsonElement goal) =>
            Assert.Equal(
                ["profileId", "goalId", "currentAge", "retirementAge", "targetSavings", "monthlyContribution", "currentSavings"],
                goal.EnumerateObject().Select(p => p.Name).ToArray());

        private static async Task<Dictionary<string, string[]>> AssertValidationProblemAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
            Assert.True(problem.TryGetProperty("traceId", out _));
            return problem.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
        }
    }
}
