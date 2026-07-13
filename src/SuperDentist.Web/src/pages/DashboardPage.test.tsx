import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { clinicApi } from '../api/clinicApi';
import { DashboardPage } from './DashboardPage';
import { dashboardSummary } from '../test/fixtures';

describe('DashboardPage', () => {
  it('shows a loading state while the dashboard request is pending', () => {
    vi.spyOn(clinicApi, 'getDashboard').mockReturnValue(new Promise(() => undefined));

    render(<DashboardPage />);

    expect(screen.getByText('Loading dashboard')).toBeInTheDocument();
  });

  it('renders successful dashboard metrics', async () => {
    vi.spyOn(clinicApi, 'getDashboard').mockResolvedValue(dashboardSummary);

    render(<DashboardPage />);

    expect(await screen.findByText('42')).toBeInTheDocument();
    expect(screen.getByText('Active doctors')).toBeInTheDocument();
    expect(screen.getByText('Grace Hopper')).toBeInTheDocument();
    expect(screen.getByText(/\$5,400/)).toBeInTheDocument();
  });

  it('renders the empty upcoming appointments state', async () => {
    vi.spyOn(clinicApi, 'getDashboard').mockResolvedValue({
      ...dashboardSummary,
      upcomingAppointments: [],
    });

    render(<DashboardPage />);

    expect(await screen.findByText('No upcoming appointments')).toBeInTheDocument();
  });

  it('shows an API error and retries the request', async () => {
    const request = vi
      .spyOn(clinicApi, 'getDashboard')
      .mockRejectedValueOnce(new Error('API unavailable'))
      .mockResolvedValueOnce(dashboardSummary);

    render(<DashboardPage />);

    expect(await screen.findByText('API unavailable')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => expect(request).toHaveBeenCalledTimes(2));
    expect(await screen.findByText('42')).toBeInTheDocument();
  });
});
