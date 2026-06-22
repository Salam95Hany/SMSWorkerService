using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class SmsGateInboxMessage
    {
        public string Id { get; set; }
        public string ContentPreview { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Sender { get; set; }
        public int SimNumber { get; set; }
        public string Type { get; set; }
    }
}
