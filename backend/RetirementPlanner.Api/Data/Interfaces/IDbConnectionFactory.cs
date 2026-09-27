using System.Data.Common;

namespace RetirementPlanner.Data.Interfaces
{
    /// <summary>The only way the application obtains a database connection.</summary>
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}
