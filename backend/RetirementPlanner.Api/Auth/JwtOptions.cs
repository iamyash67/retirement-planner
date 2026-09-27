using System.Text;

namespace RetirementPlanner.Auth
{
    /// <summary>
    /// Settings from the "Jwt" configuration section. SigningKey is a secret: it comes from user-secrets
    /// (Jwt:SigningKey) or the environment variable Jwt__SigningKey, never from appsettings.json.
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";
        public const int MinimumKeyBytes = 32; // HMAC-SHA256 needs a key of at least 256 bits

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;

        /// <summary>
        /// How long after a refresh token is rotated a second use of it is treated as a benign race (two tabs
        /// refreshing at once) instead of theft: the request is refused, but the family is not revoked.
        /// </summary>
        public int RefreshTokenReuseGraceSeconds { get; set; } = 10;

        /// <summary>Reads and validates the section, so a missing or weak key stops the API at startup.</summary>
        public static JwtOptions Load(IConfiguration configuration)
        {
            var options = configuration.GetSection(SectionName).Get<JwtOptions>() ?? new JwtOptions();
            options.Validate();
            return options;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SigningKey))
                throw new InvalidOperationException(
                    "Jwt:SigningKey is not configured. Set it with " +
                    "`dotnet user-secrets set \"Jwt:SigningKey\" \"<random 32+ byte value>\"` " +
                    "or the environment variable Jwt__SigningKey.");

            if (Encoding.UTF8.GetByteCount(SigningKey) < MinimumKeyBytes)
                throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinimumKeyBytes} bytes.");

            if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

            if (AccessTokenMinutes <= 0 || RefreshTokenDays <= 0)
                throw new InvalidOperationException("Jwt token lifetimes must be positive.");

            if (RefreshTokenReuseGraceSeconds < 0)
                throw new InvalidOperationException("Jwt:RefreshTokenReuseGraceSeconds cannot be negative.");
        }
    }
}
