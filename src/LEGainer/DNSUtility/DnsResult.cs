using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LEGainer.DNSUtility
{
    public class DnsResult
    {
        public bool Success { get; set; }

        public string RecordValue { get; set; }

        public string ErrorMessage { get; set; }

    }
}
