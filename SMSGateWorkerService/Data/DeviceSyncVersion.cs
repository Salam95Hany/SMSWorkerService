using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Data
{
    public class DeviceSyncVersion
    {
        public int DeviceSyncVersionId { get; set; }
        public int CurrentVersion { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
