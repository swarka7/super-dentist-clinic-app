export type StatusTone = 'positive' | 'warning' | 'neutral' | 'info';

interface StatusBadgeProps {
  value: string;
  tone?: StatusTone;
}

export function StatusBadge({ value, tone = 'neutral' }: StatusBadgeProps) {
  return <span className={`status-badge status-badge--${tone}`}>{value}</span>;
}
