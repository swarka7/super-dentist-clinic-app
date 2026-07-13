import { type FormEvent, useCallback, useEffect, useRef, useState } from 'react';
import { Eye, Filter, X } from 'lucide-react';
import { clinicApi } from '../api/clinicApi';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncStates';
import { PageHeader } from '../components/PageHeader';
import { StatusBadge } from '../components/StatusBadge';
import { useAsyncResource } from '../hooks/useAsyncResource';
import { operationTone } from '../utils/audit';
import type { AuditEntry, AuditOperation } from '../types/api';
import {
  formatAuditJson,
  formatLocalDateTime,
  localInputToUtc,
} from '../utils/formatters';

interface AuditFilters {
  entityType: string;
  entityId: string;
  actor: string;
  operation: '' | AuditOperation;
  fromLocal: string;
  toLocal: string;
  limit: number;
}

const emptyFilters: AuditFilters = {
  entityType: '',
  entityId: '',
  actor: '',
  operation: '',
  fromLocal: '',
  toLocal: '',
  limit: 50,
};

const entityTypes = ['Doctor', 'Patient', 'Treatment', 'Appointment', 'PatientTreatment'];

function JsonSnapshot({ title, value }: { title: string; value: string | null }) {
  const formatted = formatAuditJson(value);
  return (
    <section className="json-snapshot" aria-labelledby={`snapshot-${title}`}>
      <h3 id={`snapshot-${title}`}>{title}</h3>
      {formatted.content ? (
        <>
          {formatted.malformed ? (
            <p className="json-warning">Stored value is not valid JSON and is shown as plain text.</p>
          ) : null}
          <pre>{formatted.content}</pre>
        </>
      ) : (
        <p className="quiet-text">No values recorded.</p>
      )}
    </section>
  );
}

function AuditDetail({ entry, onClose }: { entry: AuditEntry; onClose: () => void }) {
  const closeButton = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    closeButton.current?.focus();
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [onClose]);

  return (
    <div className="dialog-backdrop" role="presentation">
      <section
        className="detail-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="audit-detail-title"
      >
        <header>
          <div>
            <span className="eyebrow">Audit entry {entry.id}</span>
            <h2 id="audit-detail-title">{entry.entityType} {entry.entityId}</h2>
          </div>
          <button
            ref={closeButton}
            className="icon-button"
            type="button"
            title="Close details"
            aria-label="Close audit details"
            onClick={onClose}
          >
            <X aria-hidden="true" size={20} />
          </button>
        </header>
        <dl className="detail-metadata">
          <div><dt>Operation</dt><dd><StatusBadge value={entry.operation} tone={operationTone(entry.operation)} /></dd></div>
          <div><dt>Actor</dt><dd>{entry.actor}</dd></div>
          <div><dt>Local time</dt><dd>{formatLocalDateTime(entry.timestampUtc)}</dd></div>
          <div><dt>Stored UTC</dt><dd>{entry.timestampUtc}</dd></div>
          <div className="detail-metadata__wide"><dt>Correlation ID</dt><dd>{entry.correlationId}</dd></div>
        </dl>
        <div className="json-grid">
          <JsonSnapshot title="Before" value={entry.oldValues} />
          <JsonSnapshot title="After" value={entry.newValues} />
        </div>
      </section>
    </div>
  );
}

export function AuditPage() {
  const [draft, setDraft] = useState<AuditFilters>(emptyFilters);
  const [filters, setFilters] = useState<AuditFilters>(emptyFilters);
  const [selected, setSelected] = useState<AuditEntry>();
  const [filterError, setFilterError] = useState('');

  const loadAudit = useCallback(
    (signal: AbortSignal) =>
      clinicApi.getAudit(
        {
          entityType: filters.entityType,
          entityId: filters.entityId,
          actor: filters.actor,
          operation: filters.operation || undefined,
          fromUtc: localInputToUtc(filters.fromLocal),
          toUtc: localInputToUtc(filters.toLocal),
          limit: filters.limit,
        },
        signal,
      ),
    [filters],
  );
  const resource = useAsyncResource(loadAudit);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (draft.fromLocal && draft.toLocal && draft.fromLocal > draft.toLocal) {
      setFilterError('From time must be on or before to time.');
      return;
    }

    setFilterError('');
    setFilters({
      ...draft,
      entityId: draft.entityId.trim(),
      actor: draft.actor.trim(),
    });
    setSelected(undefined);
  }

  function clearFilters() {
    setDraft(emptyFilters);
    setFilters(emptyFilters);
    setSelected(undefined);
    setFilterError('');
  }

  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  return (
    <div className="page-stack">
      <PageHeader
        title="Audit history"
        description="Append-only application change records"
        meta={`Stored in UTC · shown in ${timeZone}`}
      />

      <form className="filter-bar filter-bar--wide" onSubmit={applyFilters}>
        <label className="field">
          <span>Entity type</span>
          <select
            value={draft.entityType}
            onChange={(event) => setDraft((value) => ({ ...value, entityType: event.target.value }))}
          >
            <option value="">All entity types</option>
            {entityTypes.map((entityType) => <option key={entityType}>{entityType}</option>)}
          </select>
        </label>
        <label className="field">
          <span>Operation</span>
          <select
            value={draft.operation}
            onChange={(event) => setDraft((value) => ({
              ...value,
              operation: event.target.value as '' | AuditOperation,
            }))}
          >
            <option value="">All operations</option>
            <option value="Created">Created</option>
            <option value="Updated">Updated</option>
            <option value="Deleted">Deleted</option>
          </select>
        </label>
        <label className="field">
          <span>Actor</span>
          <input
            value={draft.actor}
            placeholder="LocalUser"
            onChange={(event) => setDraft((value) => ({ ...value, actor: event.target.value }))}
          />
        </label>
        <label className="field">
          <span>Entity ID</span>
          <input
            value={draft.entityId}
            placeholder="Clinic identifier"
            onChange={(event) => setDraft((value) => ({ ...value, entityId: event.target.value }))}
          />
        </label>
        <label className="field">
          <span>From (local)</span>
          <input
            type="datetime-local"
            value={draft.fromLocal}
            onChange={(event) => setDraft((value) => ({ ...value, fromLocal: event.target.value }))}
          />
        </label>
        <label className="field">
          <span>To (local)</span>
          <input
            type="datetime-local"
            value={draft.toLocal}
            onChange={(event) => setDraft((value) => ({ ...value, toLocal: event.target.value }))}
          />
        </label>
        <label className="field field--compact">
          <span>Result limit</span>
          <select
            value={draft.limit}
            onChange={(event) => setDraft((value) => ({ ...value, limit: Number(event.target.value) }))}
          >
            <option value={25}>25</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
            <option value={200}>200</option>
          </select>
        </label>
        <button className="button button--primary" type="submit">
          <Filter aria-hidden="true" size={16} />
          Apply
        </button>
        <button className="button button--secondary" type="button" onClick={clearFilters}>
          <X aria-hidden="true" size={16} />
          Clear
        </button>
        {filterError ? <p className="filter-error" role="alert">{filterError}</p> : null}
      </form>

      {resource.isLoading ? <LoadingState label="Loading audit history" /> : null}
      {resource.error ? <ErrorState error={resource.error} onRetry={resource.retry} /> : null}
      {resource.data ? (
        <section className="data-section" aria-labelledby="audit-list-title">
          <div className="section-heading">
            <div>
              <h2 id="audit-list-title">Recorded changes</h2>
              <p>{resource.data.count} newest matching records</p>
            </div>
          </div>
          {resource.data.items.length === 0 ? (
            <EmptyState
              title="No audit records found"
              message="No audit entries match the current filters."
            />
          ) : (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th scope="col">Timestamp (local)</th>
                    <th scope="col">Actor</th>
                    <th scope="col">Operation</th>
                    <th scope="col">Entity</th>
                    <th scope="col">Correlation ID</th>
                    <th scope="col"><span className="sr-only">Inspect</span></th>
                  </tr>
                </thead>
                <tbody>
                  {resource.data.items.map((entry) => (
                    <tr key={entry.id}>
                      <td><time dateTime={entry.timestampUtc}>{formatLocalDateTime(entry.timestampUtc)}</time></td>
                      <td>{entry.actor}</td>
                      <td><StatusBadge value={entry.operation} tone={operationTone(entry.operation)} /></td>
                      <td>
                        <strong>{entry.entityType}</strong>
                        <span className="cell-detail">{entry.entityId}</span>
                      </td>
                      <td><code>{entry.correlationId}</code></td>
                      <td className="table-action">
                        <button
                          className="icon-button"
                          type="button"
                          title="Inspect audit entry"
                          aria-label={`Inspect audit entry ${entry.id}`}
                          onClick={() => setSelected(entry)}
                        >
                          <Eye aria-hidden="true" size={18} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      ) : null}

      {selected ? <AuditDetail entry={selected} onClose={() => setSelected(undefined)} /> : null}
    </div>
  );
}
