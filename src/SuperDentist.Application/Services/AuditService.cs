using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class AuditService : IAuditService
    {
        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        private readonly IAuditRepository _repository;
        private readonly ICurrentActorProvider _actorProvider;
        private readonly TimeProvider _timeProvider;

        public AuditService(
            IAuditRepository repository,
            ICurrentActorProvider actorProvider,
            TimeProvider timeProvider)
        {
            _repository = repository;
            _actorProvider = actorProvider;
            _timeProvider = timeProvider;
        }

        public Task RecordAsync(
            string entityType,
            string entityId,
            AuditOperation operation,
            IReadOnlyDictionary<string, object?>? oldValues,
            IReadOnlyDictionary<string, object?>? newValues,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
            ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

            if (!Enum.IsDefined(typeof(AuditOperation), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported audit operation.");
            }

            string actor = _actorProvider.Actor;
            ArgumentException.ThrowIfNullOrWhiteSpace(actor);

            var entry = new AuditEntry
            {
                EntityType = entityType.Trim(),
                EntityId = entityId.Trim(),
                Operation = operation,
                Actor = actor.Trim(),
                TimestampUtc = _timeProvider.GetUtcNow().UtcDateTime,
                OldValues = Serialize(oldValues),
                NewValues = Serialize(newValues),
                CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                    ? Guid.NewGuid().ToString("N")
                    : correlationId.Trim()
            };

            return _repository.AddAsync(entry, cancellationToken);
        }

        public Task<IReadOnlyList<AuditEntry>> SearchAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var normalizedQuery = new AuditQuery
            {
                EntityType = query.EntityType,
                EntityId = query.EntityId,
                Actor = query.Actor,
                Operation = query.Operation,
                FromUtc = query.FromUtc,
                ToUtc = query.ToUtc,
                Limit = Math.Clamp(query.Limit, 1, 500)
            };

            return _repository.SearchAsync(normalizedQuery, cancellationToken);
        }

        private static string? Serialize(IReadOnlyDictionary<string, object?>? values)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            var orderedValues = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach ((string name, object? value) in values)
            {
                orderedValues.Add(name, value);
            }

            return JsonSerializer.Serialize(orderedValues, SerializerOptions);
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}