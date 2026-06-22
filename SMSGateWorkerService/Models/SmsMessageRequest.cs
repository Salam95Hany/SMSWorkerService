using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class SmsMessageRequest
    {
        public string SmsGateId { get; set; }
        public Guid CustomerId { get; set; }
        public int BranchId { get; set; }
        public string Message { get; set; }
        public string DeviceName { get; set; }
        public string ProviderPhone { get; set; }
        public string ProviderName { get; set; }
        public string SimNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
