using System.Data.Common;
using RetirementPlanner.Data.Interfaces;

namespace RetirementPlanner.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private DbConnection? _connection;
        private bool _disposed;

        public UnitOfWork(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DbTransaction? Transaction { get; private set; }

        public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _connection ??= await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        }

        public async Task BeginAsync(CancellationToken cancellationToken = default)
        {
            if (Transaction != null)
                throw new InvalidOperationException("A transaction is already active.");

            var connection = await GetConnectionAsync(cancellationToken);
            Transaction = await connection.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            var transaction = Transaction ?? throw new InvalidOperationException("No active transaction to commit.");
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await EndTransactionAsync();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            var transaction = Transaction ?? throw new InvalidOperationException("No active transaction to roll back.");
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await EndTransactionAsync();
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default)
        {
            await BeginAsync(cancellationToken);
            T result;
            try
            {
                result = await work();
            }
            catch
            {
                // Don't pass the caller's token: the rollback must run even if the request was cancelled.
                await RollbackAsync(CancellationToken.None);
                throw;
            }

            await CommitAsync(cancellationToken);
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            // Disposing an uncommitted transaction rolls it back.
            await EndTransactionAsync();
            if (_connection != null)
                await _connection.DisposeAsync();

            GC.SuppressFinalize(this);
        }

        private async Task EndTransactionAsync()
        {
            if (Transaction != null)
            {
                await Transaction.DisposeAsync();
                Transaction = null;
            }
        }
    }
}
