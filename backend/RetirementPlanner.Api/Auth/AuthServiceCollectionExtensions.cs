using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RetirementPlanner.Auth
{
    public static class AuthServiceCollectionExtensions
    {
        /// <summary>
        /// JWT bearer authentication, a fallback policy that requires an authenticated user on every endpoint
        /// (opt out with [AllowAnonymous]), and the rate limit for the auth endpoints.
        /// </summary>
        public static IServiceCollection AddJwtAuth(
            this IServiceCollection services, JwtOptions jwtOptions, IConfiguration configuration)
        {
            services.AddSingleton(jwtOptions);
            services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Keep "sub" as "sub" instead of mapping it to the long ClaimTypes.NameIdentifier URI.
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = JwtAccessTokenService.CreateValidationParameters(jwtOptions);
                });

            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            var rateLimit = configuration.GetSection(AuthRateLimitOptions.SectionName).Get<AuthRateLimitOptions>()
                ?? new AuthRateLimitOptions();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Fixed window per client IP. Behind a reverse proxy, configure forwarded headers so this
                // is the client's address and not the proxy's.
                options.AddPolicy(AuthRateLimitOptions.PolicyName, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimit.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var response = context.HttpContext.Response;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                    await response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests. Try again later."
                    }, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
                };
            });

            return services;
        }
    }
}
