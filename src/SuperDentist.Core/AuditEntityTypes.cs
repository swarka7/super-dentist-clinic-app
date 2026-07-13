using System.Collections.Generic;

namespace SuperDentist.Core
{
    public static class AuditEntityTypes
    {
        public const string Doctor = "Doctor";
        public const string Patient = "Patient";
        public const string Treatment = "Treatment";
        public const string Appointment = "Appointment";
        public const string PatientTreatment = "PatientTreatment";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            Doctor,
            Patient,
            Treatment,
            Appointment,
            PatientTreatment
        };
    }
}
