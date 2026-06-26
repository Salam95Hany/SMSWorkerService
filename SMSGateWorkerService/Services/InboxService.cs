using SMSGateWorkerService.Data;
using SMSGateWorkerService.ErrorLog;
using SMSGateWorkerService.Models;
using System.Text.Json;

namespace SMSGateWorkerService.Services
{
    public class InboxService
    {
        private readonly IConfiguration _configuration;
        List<string> ProviderContain;
        Guid CustomerId;
        int BranchId;
        public InboxService(IConfiguration configuration)
        {
            _configuration = configuration;
            CustomerId = _configuration.GetValue<Guid>("CustomerId");
            BranchId = _configuration.GetValue<int>("BranchId");
            ProviderContain = _configuration.GetSection("ProviderContain").Get<List<string>>();
        }

        public async Task RefreshDeviceInbox(AgentDbContext _context, SmsGateService smsGate, CloudService cloud)
        {
            var Json = await cloud.GetDeviceRefreshInboxSetting(CustomerId, BranchId);
            if (!string.IsNullOrEmpty(Json))
            {
                var Options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var Response = JsonSerializer.Deserialize<RefreshInbox>(Json, Options);
                var Device = _context.SmsGateDevices.FirstOrDefault(i => i.DeviceUniqueId == Response.DeviceId);
                await smsGate.RefreshInbox(Device, Response.From, Response.To);
                await SyncDevice(Device, Response, smsGate, cloud);
                await cloud.UpdateDeviceRefreshInboxStatus(CustomerId, BranchId, Response.DeviceId);
            }
        }

        private async Task SyncDevice(SmsGateDevice device, RefreshInbox inbox, SmsGateService smsGate, CloudService cloud)
        {
            try
            {
                var syncTime = DateTime.Now;
                int offset = 0;
                const int limit = 100;
                while (true)
                {
                    var json = await smsGate.GetInbox(device, inbox.From, inbox.To, limit, offset);
                    if (string.IsNullOrEmpty(json))
                        break;

                    var messages = ParseMessages(json, device.DeviceUniqueId).Where(i => ProviderContain.Contains(i.Sender)).ToList();
                    if (messages.Count == 0)
                        break;

                    var Params = messages.Select(i =>
                    {
                        return new SmsMessageRequest
                        {
                            SmsGateId = i.SmsGateId,
                            CustomerId = device.CustomerId,
                            BranchId = device.BranchId,
                            Message = i.ContentPreview,
                            DeviceName = i.SimNumber == 1 ? device.Sim1Name : device.Sim2Name,
                            ProviderPhone = i.SimNumber == 1 ? device.Sim1Number : device.Sim2Number,
                            ProviderName = i.Sender,
                            SimNumber = i.SimNumber.ToString(),
                            CreatedAt = i.CreatedAt
                        };
                    }).ToList();
                    var success = await cloud.SendRefreshBulkSms(Params);

                    if (messages.Count < limit)
                        break;

                    offset += limit;
                }
            }
            catch (Exception ex)
            {
                ex.AddException("InboxService", "SyncDevice", CustomerId, BranchId, device.DeviceUniqueId);
                throw;
            }
        }

        private List<SmsMessage> ParseMessages(string json, string deviceId)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var smsGateMessages = JsonSerializer.Deserialize<List<SmsGateInboxMessage>>(json, options);

            if (smsGateMessages == null)
                return new List<SmsMessage>();

            return smsGateMessages.Select(x => new SmsMessage
            {
                SmsGateId = x.Id,
                DeviceId = deviceId,
                ContentPreview = x.ContentPreview,
                Sender = x.Sender,
                SimNumber = x.SimNumber,
                CreatedAt = x.CreatedAt
            }).ToList();
        }
    }
}
