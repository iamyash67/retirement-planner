using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RetirementPlanner.Tests.Api
{
    public static class HttpAssert
    {
        /// <summary>Asserts a 400 ValidationProblemDetails and returns its errors by field.</summary>
        public static async Task<Dictionary<string, string[]>> ValidationProblemAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
            return problem.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
        }

        /// <summary>Asserts a status code with a plain-string body (the API's 401, 404 and 409 responses).</summary>
        public static async Task PlainAsync(HttpResponseMessage response, HttpStatusCode status, string message)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(message, await response.Content.ReadAsStringAsync());
        }

        public static string[] PropertyNames(JsonElement element) =>
            element.EnumerateObject().Select(p => p.Name).ToArray();
    }
}
