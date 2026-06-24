using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Models
{
    public class SystemPing
    {
        public Dictionary<string, HealthCheck> Checks { get; set; }
        public int ReleaseId { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
    }

    public class HealthCheck
    {
        public string Description { get; set; }
        public string ObservedUnit { get; set; }
        public int ObservedValue { get; set; }
        public string Status { get; set; }
    }

    public class DeviceHealthDetails
    {
        public string DeviceId { get; set; }
        public Guid CustomerId { get; set; }
        public int BranchId { get; set; }
        public string? DeviceName { get; set; }
        public int MessagesFailed { get; set; }
        public int ConnectionStatus { get; set; }
        public int ConnectionTransport { get; set; }
        public int BatteryLevel { get; set; }
        public bool BatteryCharging { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime LastSyncDate { get; set; }
    }
}
