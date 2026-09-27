using System.Data.Common;

namespace RetirementPlanner.Data.Interfaces
{
    /// <summary>
    /// One connection and at most one transaction per request, shared by every repository in the scope.
    /// </summary>
    public interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>The current transaction, or null when none is active. Pass it to every command.</summary>
        DbTransaction? Transaction { get; }

        /// <summary>Opens the connection on first use and returns the same connection afterwards.</summary>
        Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        Task BeginAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>Runs <paramref name="work"/> in a transaction: commits on success, rolls back and rethrows on failure.</summary>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default);
    }
}
