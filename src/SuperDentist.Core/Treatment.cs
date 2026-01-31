using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDentist.Core
{
    public class Treatment
    {
        public string Number { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Tools { get; set; } = string.Empty;
    }
}



