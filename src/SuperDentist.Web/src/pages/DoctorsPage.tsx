import { type FormEvent, useCallback, useState } from 'react';
import { Search, X } from 'lucide-react';
import { clinicApi } from '../api/clinicApi';
import { EmptyState, ErrorState, LoadingState } from '../components/AsyncStates';
import { PageHeader } from '../components/PageHeader';
import { Pagination } from '../components/Pagination';
import { useAsyncResource } from '../hooks/useAsyncResource';
import { formatName, safeText } from '../utils/formatters';

const PAGE_SIZE = 10;

export function DoctorsPage() {
  const [searchDraft, setSearchDraft] = useState('');
  const [search, setSearch] = useState('');
  const [offset, setOffset] = useState(0);

  const loadDoctors = useCallback(
    (signal: AbortSignal) => clinicApi.getDoctors({ search, limit: PAGE_SIZE, offset }, signal),
    [offset, search],
  );
  const resource = useAsyncResource(loadDoctors);

  function applySearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSearch(searchDraft.trim());
    setOffset(0);
  }

  function clearSearch() {
    setSearchDraft('');
    setSearch('');
    setOffset(0);
  }

  return (
    <div className="page-stack">
      <PageHeader title="Doctors" description="Clinic practitioners and specialties" />

      <form className="filter-bar" role="search" onSubmit={applySearch}>
        <label className="field field--grow">
          <span>Search doctors</span>
          <input
            type="search"
            value={searchDraft}
            placeholder="Name, ID, email, or specialty"
            onChange={(event) => setSearchDraft(event.target.value)}
          />
        </label>
        <button className="button button--primary" type="submit">
          <Search aria-hidden="true" size={16} />
          Search
        </button>
        {(search || searchDraft) && (
          <button className="button button--secondary" type="button" onClick={clearSearch}>
            <X aria-hidden="true" size={16} />
            Clear
          </button>
        )}
      </form>

      {resource.isLoading ? <LoadingState label="Loading doctors" /> : null}
      {resource.error ? <ErrorState error={resource.error} onRetry={resource.retry} /> : null}
      {resource.data ? (
        <section className="data-section" aria-labelledby="doctor-list-title">
          <div className="section-heading">
            <div>
              <h2 id="doctor-list-title">Doctor directory</h2>
              <p>{resource.data.totalCount} matching records</p>
            </div>
          </div>
          {resource.data.items.length === 0 ? (
            <EmptyState
              title="No doctors found"
              message="No doctor records match the current search."
            />
          ) : (
            <>
              <div className="table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th scope="col">Doctor</th>
                      <th scope="col">Specialization</th>
                      <th scope="col">Contact</th>
                      <th scope="col">Address</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resource.data.items.map((doctor) => (
                      <tr key={doctor.id}>
                        <td>
                          <strong>{formatName(doctor.firstName, doctor.lastName)}</strong>
                          <span className="cell-detail">{doctor.id}</span>
                        </td>
                        <td>{safeText(doctor.specialization)}</td>
                        <td>
                          <a href={`mailto:${doctor.email}`}>{safeText(doctor.email)}</a>
                          <span className="cell-detail">{safeText(doctor.phone)}</span>
                        </td>
                        <td>{safeText(doctor.address)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination
                limit={resource.data.limit}
                offset={resource.data.offset}
                totalCount={resource.data.totalCount}
                onOffsetChange={setOffset}
              />
            </>
          )}
        </section>
      ) : null}
    </div>
  );
}
