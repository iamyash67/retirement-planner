namespace RetirementPlanner.Auth
{
    /// <summary>The fixed-window limit for login, register and refresh, per client IP ("RateLimiting:Auth").</summary>
    public class AuthRateLimitOptions
    {
        public const string SectionName = "RateLimiting:Auth";
        public const string PolicyName = "auth";

        public int PermitLimit { get; set; } = 10;
        public int WindowSeconds { get; set; } = 60;
    }
}
