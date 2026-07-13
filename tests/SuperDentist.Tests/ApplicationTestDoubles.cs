using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Tests
{
    internal sealed class RecordingAuditRepository : IAuditRepository
    {
        public List<AuditEntry> Entries { get; } = new();
        public AuditQuery? LastQuery { get; private set; }

        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEntry>> SearchAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<AuditEntry>>(Entries);
        }
    }

    internal sealed class ThrowingAuditRepository : IAuditRepository
    {
        public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated audit persistence failure.");
        }

        public Task<IReadOnlyList<AuditEntry>> SearchAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AuditEntry>>(Array.Empty<AuditEntry>());
        }
    }

    internal sealed class ImmediateApplicationTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }

    internal sealed class FixedActorProvider : ICurrentActorProvider
    {
        public FixedActorProvider(string actor)
        {
            Actor = actor;
        }

        public string Actor { get; }
    }

    internal sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
