import { AlertCircle, Inbox, RefreshCw } from 'lucide-react';

interface LoadingStateProps {
  label?: string;
  rows?: number;
}

export function LoadingState({ label = 'Loading clinic data', rows = 4 }: LoadingStateProps) {
  return (
    <div className="loading-state" role="status" aria-live="polite">
      <span className="sr-only">{label}</span>
      {Array.from({ length: rows }, (_, index) => (
        <span className="skeleton-row" key={index} aria-hidden="true" />
      ))}
    </div>
  );
}

interface ErrorStateProps {
  error: Error;
  onRetry: () => void;
}

export function ErrorState({ error, onRetry }: ErrorStateProps) {
  return (
    <div className="message-state message-state--error" role="alert">
      <AlertCircle aria-hidden="true" size={22} />
      <div>
        <h2>Data could not be loaded</h2>
        <p>{error.message}</p>
      </div>
      <button className="button button--secondary" type="button" onClick={onRetry}>
        <RefreshCw aria-hidden="true" size={16} />
        Retry
      </button>
    </div>
  );
}

interface EmptyStateProps {
  title: string;
  message: string;
}

export function EmptyState({ title, message }: EmptyStateProps) {
  return (
    <div className="message-state message-state--empty">
      <Inbox aria-hidden="true" size={24} />
      <div>
        <h2>{title}</h2>
        <p>{message}</p>
      </div>
    </div>
  );
}
