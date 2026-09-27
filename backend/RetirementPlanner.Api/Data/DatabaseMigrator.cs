using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using RetirementPlanner.Data.Interfaces;

namespace RetirementPlanner.Data
{
    /// <summary>
    /// Runs the versioned scripts embedded from <c>Migrations/</c> (V001__..., V002__...) with DbUp.
    /// Applied scripts are recorded in the <c>schemaversions</c> table, so each runs exactly once.
    /// </summary>
    public class DatabaseMigrator : IDatabaseMigrator
    {
        private const string MigrationsNamespace = "RetirementPlanner.Migrations.";

        private readonly string _connectionString;
        private readonly ILogger<DatabaseMigrator> _logger;

        public DatabaseMigrator(string connectionString, ILogger<DatabaseMigrator> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public void Migrate()
        {
            var upgrader = DeployChanges.To
                .MySqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    name => name.StartsWith(MigrationsNamespace, StringComparison.Ordinal))
                .LogTo(new UpgradeLogAdapter(_logger))
                .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                throw new InvalidOperationException(
                    $"Database migration failed on script '{result.ErrorScript?.Name}'.", result.Error);
            }
        }

        /// <summary>DbUp logs with composite format strings, so they are formatted before reaching ILogger.</summary>
        private sealed class UpgradeLogAdapter : IUpgradeLog
        {
            private readonly ILogger _logger;

            public UpgradeLogAdapter(ILogger logger) => _logger = logger;

            public void LogTrace(string format, params object[] args) => _logger.LogTrace("{Message}", Format(format, args));
            public void LogDebug(string format, params object[] args) => _logger.LogDebug("{Message}", Format(format, args));
            public void LogInformation(string format, params object[] args) => _logger.LogInformation("{Message}", Format(format, args));
            public void LogWarning(string format, params object[] args) => _logger.LogWarning("{Message}", Format(format, args));
            public void LogError(string format, params object[] args) => _logger.LogError("{Message}", Format(format, args));
            public void LogError(Exception ex, string format, params object[] args) => _logger.LogError(ex, "{Message}", Format(format, args));

            private static string Format(string format, object[] args) =>
                args.Length == 0 ? format : string.Format(format, args);
        }
    }
}
