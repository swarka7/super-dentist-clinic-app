using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class SqliteTransactionScopeTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenNested_ReusesTheOwningTransaction()
        {
            using var database = await SqliteTestDatabase.CreateAsync();

            await database.Transaction.ExecuteAsync(async cancellationToken =>
            {
                SqliteConnection outerConnection;
                SqliteTransaction outerTransaction;
                await using (var outerScope = await database.ConnectionFactory.OpenScopeAsync(cancellationToken))
                {
                    outerConnection = outerScope.Connection;
                    outerTransaction = Assert.IsType<SqliteTransaction>(outerScope.Transaction);
                }

                return await database.Transaction.ExecuteAsync(async nestedCancellationToken =>
                {
                    await using var nestedScope =
                        await database.ConnectionFactory.OpenScopeAsync(nestedCancellationToken);
                    Assert.Same(outerConnection, nestedScope.Connection);
                    Assert.Same(outerTransaction, nestedScope.Transaction);
                    return true;
                }, cancellationToken);
            });
        }

        [Fact]
        public async Task OpenScopeAsync_InConcurrentTopLevelFlow_DoesNotInheritAnotherTransaction()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var firstReady = new TaskCompletionSource<SqliteConnection>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            Task first = Task.Run(() => database.Transaction.ExecuteAsync(async cancellationToken =>
            {
                await using var scope = await database.ConnectionFactory.OpenScopeAsync(cancellationToken);
                firstReady.SetResult(scope.Connection);
                await releaseFirst.Task;
                return true;
            }));

            try
            {
                SqliteConnection firstConnection = await firstReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Run(async () =>
                {
                    await using var scope = await database.ConnectionFactory.OpenScopeAsync();
                    Assert.Null(scope.Transaction);
                    Assert.NotSame(firstConnection, scope.Connection);
                });
            }
            finally
            {
                releaseFirst.TrySetResult();
                await first;
            }
        }

        [Fact]
        public async Task OpenScopeAsync_WhenChildWorkIsConcurrent_SerializesSharedConnectionAccess()
        {
            using var database = await SqliteTestDatabase.CreateAsync();

            await database.Transaction.ExecuteAsync(async cancellationToken =>
            {
                var firstScope = await database.ConnectionFactory.OpenScopeAsync(cancellationToken);
                Task<(SqliteConnection Connection, SqliteTransaction? Transaction)> second =
                    Task.Run(async () =>
                    {
                        await using var scope = await database.ConnectionFactory.OpenScopeAsync();
                        return (scope.Connection, scope.Transaction);
                    });

                await Task.Yield();
                Assert.False(second.IsCompleted);
                SqliteConnection firstConnection = firstScope.Connection;
                SqliteTransaction? firstTransaction = firstScope.Transaction;
                await firstScope.DisposeAsync();

                var secondScope = await second.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Same(firstConnection, secondScope.Connection);
                Assert.Same(firstTransaction, secondScope.Transaction);
                return true;
            });
        }

        [Fact]
        public async Task OpenScopeAsync_WhenInheritedContextHasCompleted_DoesNotReuseStaleTransaction()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var releaseChild = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Task<SqliteTransaction?>? child = null;

            await database.Transaction.ExecuteAsync(cancellationToken =>
            {
                child = Task.Run(async () =>
                {
                    await releaseChild.Task;
                    await using var scope = await database.ConnectionFactory.OpenScopeAsync();
                    return scope.Transaction;
                });

                return Task.FromResult(true);
            });

            releaseChild.SetResult();
            Assert.NotNull(child);
            Assert.Null(await child!.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        [Fact]
        public async Task ExecuteAsync_AfterException_DoesNotReuseDisposedTransaction()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            SqliteTransaction? failedTransaction = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                database.Transaction.ExecuteAsync<bool>(async cancellationToken =>
                {
                    await using var scope =
                        await database.ConnectionFactory.OpenScopeAsync(cancellationToken);
                    failedTransaction = scope.Transaction;
                    throw new InvalidOperationException("Expected transaction failure.");
                }));

            await database.Transaction.ExecuteAsync(async cancellationToken =>
            {
                await using var scope = await database.ConnectionFactory.OpenScopeAsync(cancellationToken);
                Assert.NotNull(scope.Transaction);
                Assert.NotSame(failedTransaction, scope.Transaction);
                return true;
            });
        }
    }
}
