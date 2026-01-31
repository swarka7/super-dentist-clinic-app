using Microsoft.Data.Sqlite;
using SuperDentist.Infrastructure.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public abstract class SqliteRepositoryBase
    {
        private readonly ISqliteConnectionFactory _connectionFactory;

        protected SqliteRepositoryBase(ISqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            return await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
