using Microsoft.Data.Sqlite;
using System.Threading;

namespace SuperDentist.Infrastructure.Data
{
    internal sealed class SqliteTransactionContext
    {
        private int _isActive = 1;

        public SqliteTransactionContext(SqliteTransaction transaction)
        {
            Transaction = transaction;
        }

        public SqliteTransaction Transaction { get; }
        public SemaphoreSlim CommandGate { get; } = new(1, 1);
        public bool IsActive => Volatile.Read(ref _isActive) == 1;

        public void Deactivate()
        {
            Interlocked.Exchange(ref _isActive, 0);
        }
    }
}
