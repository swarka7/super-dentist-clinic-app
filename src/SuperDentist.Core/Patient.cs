using System;

namespace SuperDentist.Core
{
    public class Patient
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string TreatmentStatus { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}