import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { clinicApi } from '../api/clinicApi';
import { appointment, doctor, paged, patient, treatment } from '../test/fixtures';
import { AppointmentsPage } from './AppointmentsPage';

it('applies an appointment doctor filter', async () => {
  const appointmentRequest = vi
    .spyOn(clinicApi, 'getAppointments')
    .mockResolvedValue(paged([appointment]));
  vi.spyOn(clinicApi, 'getDoctors').mockResolvedValue(paged([doctor]));
  vi.spyOn(clinicApi, 'getPatients').mockResolvedValue(paged([patient]));
  vi.spyOn(clinicApi, 'getTreatments').mockResolvedValue(paged([treatment]));
  const user = userEvent.setup();
  render(<AppointmentsPage />);
  await screen.findByRole('option', { name: 'Ada Lovelace' });

  await user.selectOptions(screen.getByLabelText('Doctor'), doctor.id);
  fireEvent.click(screen.getByRole('button', { name: 'Apply' }));

  await waitFor(() =>
    expect(appointmentRequest).toHaveBeenLastCalledWith(
      expect.objectContaining({ doctorId: doctor.id, limit: 15, offset: 0 }),
      expect.any(AbortSignal),
    ),
  );
});
