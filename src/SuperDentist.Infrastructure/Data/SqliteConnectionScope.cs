using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    internal sealed class SqliteConnectionScope : IAsyncDisposable
    {
        private readonly bool _ownsConnection;
        private readonly SemaphoreSlim? _commandGate;
        private int _isDisposed;

        public SqliteConnectionScope(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            bool ownsConnection,
            SemaphoreSlim? commandGate = null)
        {
            Connection = connection;
            Transaction = transaction;
            _ownsConnection = ownsConnection;
            _commandGate = commandGate;
        }

        public SqliteConnection Connection { get; }
        public SqliteTransaction? Transaction { get; }

        public SqliteCommand CreateCommand()
        {
            SqliteCommand command = Connection.CreateCommand();
            command.Transaction = Transaction;
            return command;
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return ValueTask.CompletedTask;
            }

            _commandGate?.Release();
            return _ownsConnection ? Connection.DisposeAsync() : ValueTask.CompletedTask;
        }
    }
}