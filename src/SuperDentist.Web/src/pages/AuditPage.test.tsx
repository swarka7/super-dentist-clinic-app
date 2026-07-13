import { fireEvent, render, screen } from '@testing-library/react';
import { clinicApi } from '../api/clinicApi';
import { auditEntry, boundedAudit } from '../test/fixtures';
import { AuditPage } from './AuditPage';

it('opens an audit record and safely displays snapshot JSON', async () => {
  vi.spyOn(clinicApi, 'getAudit').mockResolvedValue(boundedAudit([auditEntry]));
  render(<AuditPage />);
  await screen.findByText('corr-7');

  fireEvent.click(screen.getByRole('button', { name: 'Inspect audit entry 7' }));

  expect(screen.getByRole('dialog', { name: /Doctor D1/ })).toBeInTheDocument();
  expect(screen.getByText(/"firstName": "Old"/)).toBeInTheDocument();
  expect(screen.getByText('Stored value is not valid JSON and is shown as plain text.')).toBeInTheDocument();
  expect(screen.getByText('{malformed legacy json')).toBeInTheDocument();
});
