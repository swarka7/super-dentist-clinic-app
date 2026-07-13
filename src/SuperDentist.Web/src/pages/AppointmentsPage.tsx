import { type FormEvent, useCallback, useState } from 'react';
import { Filter, X } from 'lucide-react';
import { clinicApi } from '../api/clinicApi';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncStates';
import { PageHeader } from '../components/PageHeader';
import { Pagination } from '../components/Pagination';
import { useAsyncResource } from '../hooks/useAsyncResource';
import { formatDate, formatName } from '../utils/formatters';

const PAGE_SIZE = 15;

interface AppointmentFilters {
  search: string;
  doctorId: string;
  patientId: string;
  fromDate: string;
  toDate: string;
}

const emptyFilters: AppointmentFilters = {
  search: '',
  doctorId: '',
  patientId: '',
  fromDate: '',
  toDate: '',
};

export function AppointmentsPage() {
  const [draft, setDraft] = useState<AppointmentFilters>(emptyFilters);
  const [filters, setFilters] = useState<AppointmentFilters>(emptyFilters);
  const [offset, setOffset] = useState(0);
  const [filterError, setFilterError] = useState('');

  const loadAppointments = useCallback(
    async (signal: AbortSignal) => {
      const [appointments, doctors, patients, treatments] = await Promise.all([
        clinicApi.getAppointments({ ...filters, limit: PAGE_SIZE, offset }, signal),
        clinicApi.getDoctors({ limit: 200 }, signal),
        clinicApi.getPatients({ limit: 200 }, signal),
        clinicApi.getTreatments({ limit: 200 }, signal),
      ]);

      return {
        appointments,
        doctors: doctors.items,
        patients: patients.items,
        treatments: treatments.items,
      };
    },
    [filters, offset],
  );
  const resource = useAsyncResource(loadAppointments);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (draft.fromDate && draft.toDate && draft.fromDate > draft.toDate) {
      setFilterError('From date must be on or before to date.');
      return;
    }

    setFilterError('');
    setFilters({ ...draft, search: draft.search.trim() });
    setOffset(0);
  }

  function clearFilters() {
    setDraft(emptyFilters);
    setFilters(emptyFilters);
    setFilterError('');
    setOffset(0);
  }

  const doctorsById = new Map(
    resource.data?.doctors.map((doctor) => [doctor.id, formatName(doctor.firstName, doctor.lastName)]),
  );
  const patientsById = new Map(
    resource.data?.patients.map((patient) => [
      patient.id,
      formatName(patient.firstName, patient.lastName),
    ]),
  );
  const treatmentsByNumber = new Map(
    resource.data?.treatments.map((treatment) => [treatment.number, treatment.type]),
  );

  return (
    <div className="page-stack">
      <PageHeader title="Appointments" description="Read-only clinic schedule" />

      <form className="filter-bar filter-bar--wide" onSubmit={applyFilters}>
        <label className="field field--grow">
          <span>Search appointments</span>
          <input
            type="search"
            value={draft.search}
            placeholder="Patient, doctor, treatment, or date"
            onChange={(event) => setDraft((value) => ({ ...value, search: event.target.value }))}
          />
        </label>
        <label className="field">
          <span>Doctor</span>
          <select
            value={draft.doctorId}
            onChange={(event) => setDraft((value) => ({ ...value, doctorId: event.target.value }))}
          >
            <option value="">All doctors</option>
            {resource.data?.doctors.map((doctor) => (
              <option key={doctor.id} value={doctor.id}>
                {formatName(doctor.firstName, doctor.lastName)}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Patient</span>
          <select
            value={draft.patientId}
            onChange={(event) => setDraft((value) => ({ ...value, patientId: event.target.value }))}
          >
            <option value="">All patients</option>
            {resource.data?.patients.map((patient) => (
              <option key={patient.id} value={patient.id}>
                {formatName(patient.firstName, patient.lastName)}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>From date</span>
          <input
            type="date"
            value={draft.fromDate}
            onChange={(event) => setDraft((value) => ({ ...value, fromDate: event.target.value }))}
          />
        </label>
        <label className="field">
          <span>To date</span>
          <input
            type="date"
            value={draft.toDate}
            onChange={(event) => setDraft((value) => ({ ...value, toDate: event.target.value }))}
          />
        </label>
        <button className="button button--primary" type="submit">
          <Filter aria-hidden="true" size={16} />
          Apply
        </button>
        {[...Object.values(draft), ...Object.values(filters)].some(Boolean) && (
          <button className="button button--secondary" type="button" onClick={clearFilters}>
            <X aria-hidden="true" size={16} />
            Clear
          </button>
        )}
        {filterError ? <p className="filter-error" role="alert">{filterError}</p> : null}
      </form>

      {resource.isLoading ? <LoadingState label="Loading appointments" /> : null}
      {resource.error ? <ErrorState error={resource.error} onRetry={resource.retry} /> : null}
      {resource.data ? (
        <section className="data-section" aria-labelledby="appointment-list-title">
          <div className="section-heading">
            <div>
              <h2 id="appointment-list-title">Appointment schedule</h2>
              <p>{resource.data.appointments.totalCount} matching records</p>
            </div>
          </div>
          {resource.data.appointments.items.length === 0 ? (
            <EmptyState
              title="No appointments found"
              message="No appointments match the current filters."
            />
          ) : (
            <>
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
                    {resource.data.appointments.items.map((appointment) => (
                      <tr key={`${appointment.patientId}-${appointment.date}-${appointment.time}`}>
                        <td>{formatDate(appointment.date)}</td>
                        <td>{appointment.time || 'Not available'}</td>
                        <td>
                          <strong>{patientsById.get(appointment.patientId) ?? appointment.patientId}</strong>
                          <span className="cell-detail">{appointment.patientId}</span>
                        </td>
                        <td>
                          {doctorsById.get(appointment.doctorId) ?? appointment.doctorId}
                          <span className="cell-detail">{appointment.doctorId}</span>
                        </td>
                        <td>
                          {treatmentsByNumber.get(appointment.treatmentNumber) ?? 'Not assigned'}
                          {appointment.treatmentNumber ? (
                            <span className="cell-detail">{appointment.treatmentNumber}</span>
                          ) : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination
                limit={resource.data.appointments.limit}
                offset={resource.data.appointments.offset}
                totalCount={resource.data.appointments.totalCount}
                onOffsetChange={setOffset}
              />
            </>
          )}
        </section>
      ) : null}
    </div>
  );
}
