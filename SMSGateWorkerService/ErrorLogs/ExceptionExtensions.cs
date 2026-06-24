using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.ErrorLog
{
    public static class ExceptionExtensions
    {
        public static void AddException(this Exception ex, string service, string method, Guid? customerId = null, int? branchId = null, string deviceId = null)
        {
            ex.Data["Service"] = service;
            ex.Data["Method"] = method;
            if (customerId.HasValue)
                ex.Data["CustomerId"] = customerId;
            if (branchId.HasValue)
                ex.Data["BranchId"] = branchId;
            if (!string.IsNullOrWhiteSpace(deviceId))
                ex.Data["DeviceId"] = deviceId;
        }
    }
}
