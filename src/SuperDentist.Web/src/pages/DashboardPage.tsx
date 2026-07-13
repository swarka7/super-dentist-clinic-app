import { useCallback } from 'react';
import {
  CalendarCheck,
  CalendarClock,
  CircleDollarSign,
  ClipboardCheck,
  Clock3,
  Stethoscope,
  Users,
} from 'lucide-react';
import { clinicApi } from '../api/clinicApi';
import { BarList } from '../components/BarList';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncStates';
import { MetricCard } from '../components/MetricCard';
import { PageHeader } from '../components/PageHeader';
import { StatusBadge } from '../components/StatusBadge';
import { useAsyncResource } from '../hooks/useAsyncResource';
import { operationTone } from '../utils/audit';
import {
  formatCurrency,
  formatDate,
  formatInteger,
  formatLocalDateTime,
} from '../utils/formatters';

export function DashboardPage() {
  const loadDashboard = useCallback(
    (signal: AbortSignal) =>
      clinicApi.getDashboard(
        { upcomingAppointmentLimit: 8, recentAuditLimit: 8, breakdownLimit: 10 },
        signal,
      ),
    [],
  );
  const resource = useAsyncResource(loadDashboard);

  if (resource.isLoading) {
    return (
      <div className="page-stack">
        <PageHeader title="Operations dashboard" description="Current clinic activity and workload" />
        <LoadingState label="Loading dashboard" rows={7} />
      </div>
    );
  }

  if (resource.error) {
    return (
      <div className="page-stack">
        <PageHeader title="Operations dashboard" description="Current clinic activity and workload" />
        <ErrorState error={resource.error} onRetry={resource.retry} />
      </div>
    );
  }

  const summary = resource.data;
  if (!summary) return null;

  return (
    <div className="page-stack">
      <PageHeader
        title="Operations dashboard"
        description="Current clinic activity and workload"
        meta={`Updated ${formatLocalDateTime(summary.generatedAtUtc)}`}
      />

      <section className="metric-grid" aria-label="Clinic summary metrics">
        <MetricCard label="Total patients" value={formatInteger(summary.totalPatients)} icon={Users} />
        <MetricCard
          label="Active doctors"
          value={formatInteger(summary.activeDoctorCount)}
          icon={Stethoscope}
          tone="blue"
        />
        <MetricCard
          label="Today's appointments"
          value={formatInteger(summary.todayAppointmentCount)}
          icon={CalendarCheck}
          tone="green"
        />
        <MetricCard
          label="Upcoming appointments"
          value={formatInteger(summary.upcomingAppointmentCount)}
          icon={CalendarClock}
          tone="blue"
        />
        <MetricCard
          label="Completed treatments"
          value={formatInteger(summary.completedPatientTreatmentCount)}
          icon={ClipboardCheck}
          tone="green"
        />
        <MetricCard
          label="Outstanding treatments"
          value={formatInteger(summary.outstandingPatientTreatmentCount)}
          icon={Clock3}
          tone="amber"
        />
        <MetricCard
          label="Outstanding value"
          value={formatCurrency(summary.outstandingTreatmentValue)}
          icon={CircleDollarSign}
          tone="amber"
        />
      </section>

      <div className="dashboard-grid dashboard-grid--visuals">
        <section className="data-section" aria-labelledby="doctor-utilization-title">
          <div className="section-heading">
            <div>
              <h2 id="doctor-utilization-title">Appointments by doctor</h2>
              <p>Current scheduled workload</p>
            </div>
          </div>
          <BarList
            emptyMessage="No appointment utilization data is available."
            items={summary.appointmentsByDoctor.map((item) => ({
              id: item.doctorId,
              label: item.doctorName,
              value: item.appointmentCount,
              valueLabel: formatInteger(item.appointmentCount),
            }))}
          />
        </section>

        <section className="data-section" aria-labelledby="treatment-value-title">
          <div className="section-heading">
            <div>
              <h2 id="treatment-value-title">Treatment value</h2>
              <p>Outstanding value by treatment</p>
            </div>
          </div>
          <BarList
            emptyMessage="No treatment value data is available."
            items={summary.treatmentUsage.map((item) => ({
              id: item.treatmentNumber,
              label: item.treatmentType,
              value: item.outstandingValue,
              valueLabel: formatCurrency(item.outstandingValue),
            }))}
          />
        </section>
      </div>

      <section className="data-section" aria-labelledby="upcoming-title">
        <div className="section-heading">
          <div>
            <h2 id="upcoming-title">Upcoming appointments</h2>
            <p>Next scheduled patient visits</p>
          </div>
        </div>
        {summary.upcomingAppointments.length === 0 ? (
          <EmptyState
            title="No upcoming appointments"
            message="There are no future appointments in the current schedule."
          />
        ) : (
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th scope="col">Date</th>
                  <th scope="col">Time</th>
                  <th scope="col">Patient</th>
                  <th scope="col">Doctor</th>
                  <th scope="col">Treatment</th>
                </tr>
              </thead>
              <tbody>
                {summary.upcomingAppointments.map((appointment) => (
                  <tr key={`${appointment.patientId}-${appointment.date}-${appointment.time}`}>
                    <td>{formatDate(appointment.date)}</td>
                    <td>{appointment.time}</td>
                    <td>
                      <strong>{appointment.patientName}</strong>
                      <span className="cell-detail">{appointment.patientId}</span>
                    </td>
                    <td>{appointment.doctorName}</td>
                    <td>{appointment.treatmentType || 'Not assigned'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="data-section" aria-labelledby="recent-audit-title">
        <div className="section-heading">
          <div>
            <h2 id="recent-audit-title">Recent audit activity</h2>
            <p>Newest recorded clinic changes</p>
          </div>
        </div>
        {summary.recentAuditActivity.length === 0 ? (
          <EmptyState
            title="No audit activity"
            message="No application changes have been recorded yet."
          />
        ) : (
          <ul className="activity-list">
            {summary.recentAuditActivity.map((entry) => (
              <li key={entry.id}>
                <span className="activity-list__marker" aria-hidden="true" />
                <div>
                  <strong>{entry.entityType}</strong>
                  <span>{entry.entityId}</span>
                </div>
                <StatusBadge value={entry.operation} tone={operationTone(entry.operation)} />
                <span>{entry.actor}</span>
                <time dateTime={entry.timestampUtc}>{formatLocalDateTime(entry.timestampUtc)}</time>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
