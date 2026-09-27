using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RetirementPlanner.Infrastructure;

namespace RetirementPlanner.Tests.Infrastructure
{
    public class GlobalExceptionHandlerTests
    {
        private static async Task<(HttpContext Context, JsonElement Body)> HandleAsync(string environmentName)
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
            var handler = new GlobalExceptionHandler(environment.Object, NullLogger<GlobalExceptionHandler>.Instance);

            var context = new DefaultHttpContext();
            context.Request.Path = "/api/goal/1";
            context.Response.Body = new MemoryStream();

            var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);
            Assert.True(handled);

            context.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
            return (context, body);
        }

        [Fact]
        public async Task TryHandleAsync_WritesProblemDetailsWith500()
        {
            var (context, body) = await HandleAsync(Environments.Production);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.StartsWith("application/problem+json", context.Response.ContentType);
            Assert.Equal(500, body.GetProperty("status").GetInt32());
            Assert.Equal("An unexpected error occurred.", body.GetProperty("title").GetString());
            Assert.Equal("/api/goal/1", body.GetProperty("instance").GetString());
            Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
        }

        [Fact]
        public async Task TryHandleAsync_OutsideDevelopment_HidesExceptionMessage()
        {
            var (_, body) = await HandleAsync(Environments.Production);

            Assert.False(body.TryGetProperty("detail", out _));
        }

        [Fact]
        public async Task TryHandleAsync_InDevelopment_IncludesExceptionMessage()
        {
            var (_, body) = await HandleAsync(Environments.Development);

            Assert.Equal("boom", body.GetProperty("detail").GetString());
        }
    }
}
