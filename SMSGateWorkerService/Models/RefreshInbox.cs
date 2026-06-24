using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class RefreshInbox
    {
        public string DeviceId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
