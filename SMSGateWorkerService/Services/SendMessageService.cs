using Microsoft.EntityFrameworkCore;
using SMSGateWorkerService.Data;
using SMSGateWorkerService.ErrorLog;
using SMSGateWorkerService.Models;
using System.Text.Json;

namespace SMSGateWorkerService.Services
{
    public class SendMessageService
    {
        private readonly IConfiguration _configuration;
        List<string> ProviderContain;
        public SendMessageService(IConfiguration configuration)
        {
            _configuration = configuration;
            ProviderContain = _configuration.GetSection("ProviderContain").Get<List<string>>();
        }

        public async Task SyncSendMessages(AgentDbContext _context, SmsGateService smsGate, CloudService cloud)
        {
            var devices = _context.SmsGateDevices.ToList();
            foreach (var device in devices)
                await SyncDevice(device, _context, smsGate);

            await _context.SaveChangesAsync();
            await SendPendingMessages(_context, cloud);

        }

        private async Task SyncDevice(SmsGateDevice device, AgentDbContext db, SmsGateService smsGate)
        {
            try
            {
                var syncTime = DateTime.Now;
                int offset = 0;
                const int limit = 100;
                bool syncCompleted = true;
                while (true)
                {
                    var json = await smsGate.GetInbox(device, device.LastSyncDate, syncTime, limit, offset);
                    if (string.IsNullOrEmpty(json))
                    {
                        syncCompleted = false;
                        break;
                    }


                    var messages = ParseMessages(json, device.DeviceUniqueId).Where(i => ProviderContain.Contains(i.Sender)).ToList();
                    if (messages.Count == 0)
                        break;

                    var ids = messages.Select(x => x.SmsGateId).ToList();
                    var existingIds = db.SmsMessages.Where(x => ids.Contains(x.SmsGateId)).Select(x => x.SmsGateId).ToHashSet();
                    var newMessages = messages.Where(x => !existingIds.Contains(x.SmsGateId)).ToList();
                    db.SmsMessages.AddRange(newMessages);

                    if (messages.Count < limit)
                        break;

                    offset += limit;
                }

                if (syncCompleted)
                    device.LastSyncDate = syncTime;
            }
            catch (Exception ex)
            {
                ex.AddException("SendMessageService", "SyncDevice", device.CustomerId, device.BranchId, device.DeviceUniqueId);
                throw;
            }
        }



        private async Task SendPendingMessages(AgentDbContext db, CloudService cloud)
        {
            try
            {
                var pending = await db.SmsMessages.Where(x => !x.SentToCloud).ToListAsync();
                if (pending.Count == 0)
                    return;

                var deviceIds = pending.Select(x => x.DeviceId).Distinct().ToList();
                var devices = await db.SmsGateDevices.Where(x => deviceIds.Contains(x.DeviceUniqueId)).ToDictionaryAsync(x => x.DeviceUniqueId);
                var parameters = pending.Where(x => devices.ContainsKey(x.DeviceId)).Select(x =>
                {
                    var device = devices[x.DeviceId];

                    return new SmsMessageRequest
                    {
                        SmsGateId = x.SmsGateId,
                        CustomerId = device.CustomerId,
                        BranchId = device.BranchId,
                        Message = x.ContentPreview,
                        DeviceName = x.SimNumber == 1 ? device.Sim1Name : device.Sim2Name,
                        ProviderPhone = x.SimNumber == 1 ? device.Sim1Number : device.Sim2Number,
                        ProviderName = x.Sender,
                        SimNumber = x.SimNumber.ToString(),
                        CreatedAt = x.CreatedAt
                    };
                }).ToList();
                var success = await cloud.SendBulkSms(parameters);

                if (success)
                {
                    db.SmsMessages.RemoveRange(pending);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ex.AddException("SendMessageService", "SendPendingMessages",Guid.Parse("4E4F1CDF-192C-4DB9-B16D-CB633A874FF4"), 1);
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
