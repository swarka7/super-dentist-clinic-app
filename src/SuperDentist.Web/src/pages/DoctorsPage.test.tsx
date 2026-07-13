import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { clinicApi } from '../api/clinicApi';
import { doctor, paged } from '../test/fixtures';
import { DoctorsPage } from './DoctorsPage';

describe('DoctorsPage', () => {
  it('applies a doctor search through the API boundary', async () => {
    const request = vi.spyOn(clinicApi, 'getDoctors').mockResolvedValue(paged([doctor]));
    const user = userEvent.setup();
    render(<DoctorsPage />);
    await screen.findByText('Ada Lovelace');

    await user.type(screen.getByLabelText('Search doctors'), 'Ada');
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    await waitFor(() =>
      expect(request).toHaveBeenLastCalledWith(
        expect.objectContaining({ search: 'Ada', offset: 0 }),
        expect.any(AbortSignal),
      ),
    );
  });

  it('requests the next bounded page', async () => {
    const secondDoctor = { ...doctor, id: 'D11', firstName: 'Katherine', lastName: 'Johnson' };
    const request = vi
      .spyOn(clinicApi, 'getDoctors')
      .mockResolvedValueOnce(paged([doctor], 12, 0))
      .mockResolvedValueOnce(paged([secondDoctor], 12, 10));
    render(<DoctorsPage />);
    await screen.findByText('Ada Lovelace');

    fireEvent.click(screen.getByRole('button', { name: 'Next page' }));

    expect(await screen.findByText('Katherine Johnson')).toBeInTheDocument();
    expect(request).toHaveBeenLastCalledWith(
      expect.objectContaining({ limit: 10, offset: 10 }),
      expect.any(AbortSignal),
    );
  });
});
