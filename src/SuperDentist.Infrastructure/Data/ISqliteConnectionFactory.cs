using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    public interface ISqliteConnectionFactory
    {
        string DatabasePath { get; }
        Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}
