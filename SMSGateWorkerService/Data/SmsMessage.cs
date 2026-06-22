using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Data
{
    public class SmsMessage
    {
        public int Id { get; set; }
        public string SmsGateId { get; set; }
        public string DeviceId { get; set; }
        public string ContentPreview { get; set; }
        public string Sender { get; set; }
        public int SimNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool SentToCloud { get; set; }
    }
}
