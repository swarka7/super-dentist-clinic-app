import { getJson, withQuery } from './client';
import type {
  Appointment,
  AppointmentQuery,
  AuditEntry,
  AuditQuery,
  BoundedResponse,
  DashboardQuery,
  DashboardSummary,
  Doctor,
  PagedQuery,
  PagedResponse,
  Patient,
  PatientQuery,
  Treatment,
} from '../types/api';

export const clinicApi = {
  getDashboard(query: DashboardQuery = {}, signal?: AbortSignal) {
    return getJson<DashboardSummary>(
      withQuery('/api/dashboard/summary', query),
      signal,
    );
  },

  getDoctors(query: PagedQuery = {}, signal?: AbortSignal) {
    return getJson<PagedResponse<Doctor>>(withQuery('/api/doctors', query), signal);
  },

  getPatients(query: PatientQuery = {}, signal?: AbortSignal) {
    return getJson<PagedResponse<Patient>>(withQuery('/api/patients', query), signal);
  },

  getAppointments(query: AppointmentQuery = {}, signal?: AbortSignal) {
    return getJson<PagedResponse<Appointment>>(
      withQuery('/api/appointments', query),
      signal,
    );
  },

  getTreatments(query: PagedQuery = {}, signal?: AbortSignal) {
    return getJson<PagedResponse<Treatment>>(withQuery('/api/treatments', query), signal);
  },

  getAudit(query: AuditQuery = {}, signal?: AbortSignal) {
    return getJson<BoundedResponse<AuditEntry>>(withQuery('/api/audit', query), signal);
  },
};
