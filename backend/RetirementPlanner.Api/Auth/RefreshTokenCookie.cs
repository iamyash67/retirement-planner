namespace RetirementPlanner.Auth
{
    /// <summary>
    /// The refresh token cookie: httpOnly (no script access), Secure, SameSite=Strict, and scoped to
    /// /api/auth so it is only sent to the refresh and logout endpoints.
    /// </summary>
    public static class RefreshTokenCookie
    {
        public const string Name = "rp_refresh";
        public const string Path = "/api/auth";

        public static void Append(HttpResponse response, string token, DateTimeOffset expiresAt) =>
            response.Cookies.Append(Name, token, CreateOptions(expiresAt));

        public static void Delete(HttpResponse response) =>
            response.Cookies.Delete(Name, CreateOptions(expiresAt: null));

        public static string? Read(HttpRequest request) =>
            request.Cookies.TryGetValue(Name, out var token) ? token : null;

        private static CookieOptions CreateOptions(DateTimeOffset? expiresAt) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = expiresAt,
            IsEssential = true
        };
    }
}
