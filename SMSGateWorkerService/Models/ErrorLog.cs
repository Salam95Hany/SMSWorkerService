using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class ErrorLog
    {
        public string DeviceId { get; set; }
        public Guid CustomerId { get; set; }
        public int BranchId { get; set; }
        public string Service { get; set; }
        public string Method { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
