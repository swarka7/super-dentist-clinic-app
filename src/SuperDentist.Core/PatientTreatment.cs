using System;

namespace SuperDentist.Core
{
    public class PatientTreatment
    {
        public string TreatmentNumber { get; set; } = string.Empty;
        public string IsPaid { get; set; } = string.Empty;
        public string IsCompleted { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}