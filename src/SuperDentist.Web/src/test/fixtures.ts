import type {
  Appointment,
  AuditEntry,
  BoundedResponse,
  DashboardSummary,
  Doctor,
  PagedResponse,
  Patient,
  Treatment,
} from '../types/api';

export const doctor: Doctor = {
  id: 'D1',
  firstName: 'Ada',
  lastName: 'Lovelace',
  phone: '555-0101',
  address: '1 Clinic Way',
  email: 'ada@example.test',
  specialization: 'General',
  createdAtUtc: '2026-07-14T08:00:00Z',
  updatedAtUtc: '2026-07-14T08:00:00Z',
};

export const patient: Patient = {
  id: 'P1',
  firstName: 'Grace',
  lastName: 'Hopper',
  phone: '555-0202',
  address: '2 Clinic Way',
  email: 'grace@example.test',
  age: 45,
  treatmentStatus: 'No',
  doctorId: doctor.id,
  createdAtUtc: '2026-07-14T08:00:00Z',
  updatedAtUtc: '2026-07-14T08:00:00Z',
};

export const treatment: Treatment = {
  number: 'T1',
  type: 'Cleaning',
  price: 200,
  tools: 'Standard kit',
  createdAtUtc: '2026-07-14T08:00:00Z',
  updatedAtUtc: '2026-07-14T08:00:00Z',
};

export const appointment: Appointment = {
  patientId: patient.id,
  doctorId: doctor.id,
  treatmentNumber: treatment.number,
  date: '2026-07-15',
  time: '09:30',
  createdAtUtc: '2026-07-14T08:00:00Z',
  updatedAtUtc: '2026-07-14T08:00:00Z',
};

export const auditEntry: AuditEntry = {
  id: 7,
  entityType: 'Doctor',
  entityId: doctor.id,
  operation: 'Updated',
  actor: 'LocalUser',
  timestampUtc: '2026-07-14T08:30:00Z',
  oldValues: '{"firstName":"Old","active":true,"rate":12.5}',
  newValues: '{malformed legacy json',
  correlationId: 'corr-7',
};

export const dashboardSummary: DashboardSummary = {
  generatedAtUtc: '2026-07-14T09:00:00Z',
  totalPatients: 42,
  activeDoctorCount: 6,
  todayAppointmentCount: 8,
  upcomingAppointmentCount: 12,
  completedPatientTreatmentCount: 21,
  outstandingPatientTreatmentCount: 9,
  outstandingTreatmentValue: 5400,
  appointmentsByDoctor: [
    { doctorId: doctor.id, doctorName: 'Ada Lovelace', appointmentCount: 5 },
  ],
  treatmentUsage: [
    {
      treatmentNumber: treatment.number,
      treatmentType: treatment.type,
      unitPrice: treatment.price,
      usageCount: 3,
      totalValue: 600,
      outstandingValue: 200,
    },
  ],
  upcomingAppointments: [
    {
      patientId: patient.id,
      patientName: 'Grace Hopper',
      doctorId: doctor.id,
      doctorName: 'Ada Lovelace',
      date: appointment.date,
      time: appointment.time,
      treatmentNumber: treatment.number,
      treatmentType: treatment.type,
    },
  ],
  recentAuditActivity: [
    {
      id: auditEntry.id,
      entityType: auditEntry.entityType,
      entityId: auditEntry.entityId,
      operation: auditEntry.operation,
      actor: auditEntry.actor,
      timestampUtc: auditEntry.timestampUtc,
      correlationId: auditEntry.correlationId,
    },
  ],
};

export function paged<T>(items: T[], totalCount = items.length, offset = 0): PagedResponse<T> {
  return { items, totalCount, limit: 10, offset };
}

export function boundedAudit(items: AuditEntry[]): BoundedResponse<AuditEntry> {
  return { items, count: items.length, limit: 50 };
}
