using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDentist.Core
{
    public class PatientTreatment
    {
        public string TreatmentNumber { get; set; } = string.Empty;
        public string IsPaid { get; set; } = string.Empty;
        public string IsCompleted { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
    }
}

