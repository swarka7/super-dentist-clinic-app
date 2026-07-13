using SuperDentist.Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    internal sealed class SqliteApplicationTransaction : IApplicationTransaction
    {
        private readonly SqliteConnectionFactory _connectionFactory;

        public SqliteApplicationTransaction(SqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return _connectionFactory.ExecuteInTransactionAsync(operation, cancellationToken);
        }
    }
}
