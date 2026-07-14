import { fireEvent, render, screen } from '@testing-library/react';
import { clinicApi } from '../api/clinicApi';
import { auditEntry, boundedAudit } from '../test/fixtures';
import { AuditPage } from './AuditPage';

it('opens an audit record and safely displays snapshot JSON', async () => {
  vi.spyOn(clinicApi, 'getAudit').mockResolvedValue(boundedAudit([auditEntry]));
  render(<AuditPage />);
  await screen.findByText('corr-7');

  const inspectButton = screen.getByRole('button', { name: 'Inspect audit entry 7' });
  inspectButton.focus();
  fireEvent.click(inspectButton);

  expect(screen.getByRole('dialog', { name: /Doctor D1/ })).toBeInTheDocument();
  expect(screen.getByText(/"firstName": "Old"/)).toBeInTheDocument();
  expect(screen.getByText('Stored value is not valid JSON and is shown as plain text.')).toBeInTheDocument();
  expect(screen.getByText('{malformed legacy json')).toBeInTheDocument();

  const closeButton = screen.getByRole('button', { name: 'Close audit details' });
  expect(closeButton).toHaveFocus();
  fireEvent.keyDown(window, { key: 'Tab' });
  expect(closeButton).toHaveFocus();

  fireEvent.keyDown(window, { key: 'Escape' });
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(inspectButton).toHaveFocus();
});
