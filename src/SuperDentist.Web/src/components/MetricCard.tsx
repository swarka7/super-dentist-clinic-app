import type { LucideIcon } from 'lucide-react';

interface MetricCardProps {
  label: string;
  value: string;
  icon: LucideIcon;
  tone?: 'teal' | 'blue' | 'green' | 'amber';
}

export function MetricCard({ label, value, icon: Icon, tone = 'teal' }: MetricCardProps) {
  return (
    <article className="metric-card">
      <span className={`metric-card__icon metric-card__icon--${tone}`} aria-hidden="true">
        <Icon size={19} />
      </span>
      <div>
        <p>{label}</p>
        <strong>{value}</strong>
      </div>
    </article>
  );
}
