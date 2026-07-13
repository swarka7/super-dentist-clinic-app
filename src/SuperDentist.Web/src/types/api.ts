export type AuditOperation = 'Created' | 'Updated' | 'Deleted';

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  limit: number;
  offset: number;
}

export interface BoundedResponse<T> {
  items: T[];
  count: number;
  limit: number;
}

export interface Doctor {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  address: string;
  email: string;
  specialization: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  address: string;
  email: string;
  age: number;
  treatmentStatus: string;
  doctorId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Appointment {
  patientId: string;
  doctorId: string;
  date: string;
  time: string;
  treatmentNumber: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface Treatment {
  number: string;
  type: string;
  price: number;
  tools: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AuditEntry {
  id: number;
  entityType: string;
  entityId: string;
  operation: AuditOperation;
  actor: string;
  timestampUtc: string;
  oldValues: string | null;
  newValues: string | null;
  correlationId: string;
}

export interface DoctorAppointmentSummary {
  doctorId: string;
  doctorName: string;
  appointmentCount: number;
}

export interface TreatmentUsageSummary {
  treatmentNumber: string;
  treatmentType: string;
  unitPrice: number;
  usageCount: number;
  totalValue: number;
  outstandingValue: number;
}

export interface UpcomingAppointment {
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  date: string;
  time: string;
  treatmentNumber: string;
  treatmentType: string;
}

export interface RecentAuditActivity {
  id: number;
  entityType: string;
  entityId: string;
  operation: AuditOperation;
  actor: string;
  timestampUtc: string;
  correlationId: string;
}

export interface DashboardSummary {
  generatedAtUtc: string;
  totalPatients: number;
  activeDoctorCount: number;
  todayAppointmentCount: number;
  upcomingAppointmentCount: number;
  completedPatientTreatmentCount: number;
  outstandingPatientTreatmentCount: number;
  outstandingTreatmentValue: number;
  appointmentsByDoctor: DoctorAppointmentSummary[];
  treatmentUsage: TreatmentUsageSummary[];
  upcomingAppointments: UpcomingAppointment[];
  recentAuditActivity: RecentAuditActivity[];
}

export interface PagedQuery {
  search?: string;
  limit?: number;
  offset?: number;
}

export interface PatientQuery extends PagedQuery {
  doctorId?: string;
}

export interface AppointmentQuery extends PagedQuery {
  doctorId?: string;
  patientId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface AuditQuery {
  entityType?: string;
  entityId?: string;
  actor?: string;
  operation?: AuditOperation;
  fromUtc?: string;
  toUtc?: string;
  limit?: number;
}

export interface DashboardQuery {
  upcomingAppointmentLimit?: number;
  recentAuditLimit?: number;
  breakdownLimit?: number;
}
