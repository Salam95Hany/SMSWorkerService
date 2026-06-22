using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class DeviceHealthResult
    {
        public string DeviceId { get; set; }
        public SystemPing Health { get; set; }
    }
}
