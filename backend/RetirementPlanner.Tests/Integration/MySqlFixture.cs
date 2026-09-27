using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using RetirementPlanner.Data;
using RetirementPlanner.Data.Interfaces;
using Testcontainers.MySql;

namespace RetirementPlanner.Tests.Integration
{
    /// <summary>One MySQL container per test run, migrated once. Tests isolate themselves with unique emails.</summary>
    public sealed class MySqlFixture : IAsyncLifetime
    {
        private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4").Build();

        public string ConnectionString { get; private set; } = string.Empty;
        public IDbConnectionFactory ConnectionFactory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            ConnectionFactory = new MySqlConnectionFactory(ConnectionString);
            CreateMigrator().Migrate();
        }

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        public DatabaseMigrator CreateMigrator() =>
            new(ConnectionString, NullLogger<DatabaseMigrator>.Instance);

        public UnitOfWork CreateUnitOfWork() => new(ConnectionFactory);

        public async Task<T> ScalarAsync<T>(string sql, object? parameters = null)
        {
            await using var connection = await ConnectionFactory.CreateOpenConnectionAsync();
            return await connection.ExecuteScalarAsync<T>(sql, parameters) ?? throw new InvalidOperationException("No value.");
        }

        public async Task ExecuteAsync(string sql, object? parameters = null)
        {
            await using var connection = await ConnectionFactory.CreateOpenConnectionAsync();
            await connection.ExecuteAsync(sql, parameters);
        }

        public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
    }

    [CollectionDefinition(Name)]
    public class MySqlCollection : ICollectionFixture<MySqlFixture>
    {
        public const string Name = "MySQL";
    }
}
