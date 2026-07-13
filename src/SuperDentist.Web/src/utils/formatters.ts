const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'USD',
  maximumFractionDigits: 0,
});

const integerFormatter = new Intl.NumberFormat();

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatInteger(value: number): string {
  return integerFormatter.format(value);
}

export function formatName(firstName: string, lastName: string): string {
  return `${firstName} ${lastName}`.trim() || 'Not available';
}

export function formatLocalDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return 'Not available';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

export function formatDate(value: string): string {
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) {
    return value || 'Not available';
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(
    new Date(year, month - 1, day),
  );
}

export function safeText(value: string | null | undefined): string {
  return value?.trim() || 'Not available';
}

export interface JsonDisplayValue {
  content: string | null;
  malformed: boolean;
}

export function formatAuditJson(value: string | null | undefined): JsonDisplayValue {
  if (!value?.trim()) {
    return { content: null, malformed: false };
  }

  try {
    return { content: JSON.stringify(JSON.parse(value), null, 2), malformed: false };
  } catch {
    return { content: value, malformed: true };
  }
}

export function localInputToUtc(value: string): string | undefined {
  if (!value) {
    return undefined;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}
