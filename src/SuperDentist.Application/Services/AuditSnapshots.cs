using SuperDentist.Core;
using System.Collections.Generic;

namespace SuperDentist.Application.Services
{
    internal static class AuditSnapshots
    {
        public static IReadOnlyDictionary<string, object?> Doctor(Doctor value) => Snapshot(
            ("Address", value.Address),
            ("Email", value.Email),
            ("FirstName", value.FirstName),
            ("Id", value.Id),
            ("LastName", value.LastName),
            ("Phone", value.Phone),
            ("Salary", value.Salary),
            ("Specialization", value.Specialization));

        public static IReadOnlyDictionary<string, object?> Patient(Patient value) => Snapshot(
            ("Address", value.Address),
            ("Age", value.Age),
            ("DoctorId", value.DoctorId),
            ("Email", value.Email),
            ("FirstName", value.FirstName),
            ("Id", value.Id),
            ("LastName", value.LastName),
            ("Phone", value.Phone),
            ("TreatmentStatus", value.TreatmentStatus));

        public static IReadOnlyDictionary<string, object?> Treatment(Treatment value) => Snapshot(
            ("Number", value.Number),
            ("Price", value.Price),
            ("Tools", value.Tools),
            ("Type", value.Type));

        public static IReadOnlyDictionary<string, object?> Appointment(Appointment value) => Snapshot(
            ("Date", value.Date),
            ("DoctorId", value.DoctorId),
            ("PatientId", value.PatientId),
            ("Time", value.Time),
            ("TreatmentNumber", value.TreatmentNumber));

        public static IReadOnlyDictionary<string, object?> PatientTreatment(PatientTreatment value) => Snapshot(
            ("IsCompleted", value.IsCompleted),
            ("IsPaid", value.IsPaid),
            ("PatientId", value.PatientId),
            ("StartDate", value.StartDate),
            ("TreatmentNumber", value.TreatmentNumber));

        private static IReadOnlyDictionary<string, object?> Snapshot(
            params (string Name, object? Value)[] values)
        {
            var snapshot = new SortedDictionary<string, object?>(System.StringComparer.Ordinal);
            foreach ((string name, object? value) in values)
            {
                snapshot.Add(name, value);
            }

            return snapshot;
        }
    }
}
