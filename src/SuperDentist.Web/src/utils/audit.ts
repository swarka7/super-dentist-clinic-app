import type { StatusTone } from '../components/StatusBadge';
import type { AuditOperation } from '../types/api';

export function operationTone(operation: AuditOperation): StatusTone {
  if (operation === 'Created') return 'positive';
  if (operation === 'Deleted') return 'warning';
  return 'info';
}
