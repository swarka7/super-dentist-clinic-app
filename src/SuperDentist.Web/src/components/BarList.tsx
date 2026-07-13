interface BarListItem {
  id: string;
  label: string;
  value: number;
  valueLabel: string;
}

interface BarListProps {
  items: BarListItem[];
  emptyMessage: string;
}

export function BarList({ items, emptyMessage }: BarListProps) {
  if (items.length === 0) {
    return <p className="quiet-text">{emptyMessage}</p>;
  }

  const maximum = Math.max(...items.map((item) => item.value), 1);

  return (
    <div className="bar-list">
      {items.map((item) => {
        const width = Math.max(3, (item.value / maximum) * 100);
        return (
          <div className="bar-list__item" key={item.id}>
            <div className="bar-list__label">
              <span title={item.label}>{item.label}</span>
              <strong>{item.valueLabel}</strong>
            </div>
            <div
              className="bar-list__track"
              role="img"
              aria-label={`${item.label}: ${item.valueLabel}`}
            >
              <span style={{ width: `${width}%` }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}
