using SMSGateWorkerService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class DeviceWorkerResponse
    {
        public int LatestVersion { get; set; }
        public List<SmsGateDevice> Devices { get; set; }
        public List<DeviceChangeLog> Changes { get; set; }
    }

    public class DeviceChangeLog
    {
        public int DeviceChangeLogId { get; set; }
        public string DeviceId { get; set; }
        public Guid CustomerId { get; set; }
        public int BranchId { get; set; }
        public int Version { get; set; }
        public ChangeLogAction Action { get; set; }
        public string FieldKey { get; set; }
        public string FieldValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ChangeLogAction
    {
        Created = 1,
        Updated = 2,
        Deleted = 3
    }
}
