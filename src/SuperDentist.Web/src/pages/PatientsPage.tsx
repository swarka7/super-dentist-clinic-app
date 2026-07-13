import { type FormEvent, useCallback, useState } from 'react';
import { Filter, X } from 'lucide-react';
import { clinicApi } from '../api/clinicApi';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncStates';
import { PageHeader } from '../components/PageHeader';
import { Pagination } from '../components/Pagination';
import { StatusBadge } from '../components/StatusBadge';
import { useAsyncResource } from '../hooks/useAsyncResource';
import { formatName, safeText } from '../utils/formatters';

const PAGE_SIZE = 10;

interface PatientFilters {
  search: string;
  doctorId: string;
}

const emptyFilters: PatientFilters = { search: '', doctorId: '' };

export function PatientsPage() {
  const [draft, setDraft] = useState<PatientFilters>(emptyFilters);
  const [filters, setFilters] = useState<PatientFilters>(emptyFilters);
  const [offset, setOffset] = useState(0);

  const loadPatients = useCallback(
    async (signal: AbortSignal) => {
      const [patients, doctors] = await Promise.all([
        clinicApi.getPatients({ ...filters, limit: PAGE_SIZE, offset }, signal),
        clinicApi.getDoctors({ limit: 200 }, signal),
      ]);
      return { patients, doctors: doctors.items };
    },
    [filters, offset],
  );
  const resource = useAsyncResource(loadPatients);

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFilters({ search: draft.search.trim(), doctorId: draft.doctorId });
    setOffset(0);
  }

  function clearFilters() {
    setDraft(emptyFilters);
    setFilters(emptyFilters);
    setOffset(0);
  }

  const doctorsById = new Map(
    resource.data?.doctors.map((doctor) => [doctor.id, formatName(doctor.firstName, doctor.lastName)]),
  );

  return (
    <div className="page-stack">
      <PageHeader title="Patients" description="Patient roster and treatment status" />

      <form className="filter-bar" onSubmit={applyFilters}>
        <label className="field field--grow">
          <span>Search patients</span>
          <input
            type="search"
            value={draft.search}
            placeholder="Name, ID, email, or phone"
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
        <button className="button button--primary" type="submit">
          <Filter aria-hidden="true" size={16} />
          Apply
        </button>
        {(filters.search || filters.doctorId || draft.search || draft.doctorId) && (
          <button className="button button--secondary" type="button" onClick={clearFilters}>
            <X aria-hidden="true" size={16} />
            Clear
          </button>
        )}
      </form>

      {resource.isLoading ? <LoadingState label="Loading patients" /> : null}
      {resource.error ? <ErrorState error={resource.error} onRetry={resource.retry} /> : null}
      {resource.data ? (
        <section className="data-section" aria-labelledby="patient-list-title">
          <div className="section-heading">
            <div>
              <h2 id="patient-list-title">Patient directory</h2>
              <p>{resource.data.patients.totalCount} matching records</p>
            </div>
          </div>
          {resource.data.patients.items.length === 0 ? (
            <EmptyState
              title="No patients found"
              message="No patient records match the current filters."
            />
          ) : (
            <>
              <div className="table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th scope="col">Patient</th>
                      <th scope="col">Age</th>
                      <th scope="col">Assigned doctor</th>
                      <th scope="col">Treatment status</th>
                      <th scope="col">Contact</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resource.data.patients.items.map((patient) => {
                      const isComplete = patient.treatmentStatus.trim().toLowerCase() === 'yes';
                      return (
                        <tr key={patient.id}>
                          <td>
                            <strong>{formatName(patient.firstName, patient.lastName)}</strong>
                            <span className="cell-detail">{patient.id}</span>
                          </td>
                          <td>{patient.age}</td>
                          <td>
                            {doctorsById.get(patient.doctorId) ?? patient.doctorId}
                            <span className="cell-detail">{patient.doctorId}</span>
                          </td>
                          <td>
                            <StatusBadge
                              value={isComplete ? 'Completed' : 'Outstanding'}
                              tone={isComplete ? 'positive' : 'warning'}
                            />
                          </td>
                          <td>
                            <a href={`mailto:${patient.email}`}>{safeText(patient.email)}</a>
                            <span className="cell-detail">{safeText(patient.phone)}</span>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <Pagination
                limit={resource.data.patients.limit}
                offset={resource.data.patients.offset}
                totalCount={resource.data.patients.totalCount}
                onOffsetChange={setOffset}
              />
            </>
          )}
        </section>
      ) : null}
    </div>
  );
}
