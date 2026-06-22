using SMSGateWorkerService.Data;
using SMSGateWorkerService.Models;
using SMSGateWorkerService.Services;
using System.Text.Json;

namespace SMSGateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        List<string> ProviderContain;
        public Worker(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            ProviderContain = _configuration.GetSection("ProviderContain").Get<List<string>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = _configuration.GetValue<int>("Sync:IntervalSeconds");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Sync();
                }
                catch (Exception ex)
                {
                    // TODO: Add logging
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task Sync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
            var smsGate = scope.ServiceProvider.GetRequiredService<SmsGateService>();
            var cloud = scope.ServiceProvider.GetRequiredService<CloudService>();
            var device = scope.ServiceProvider.GetRequiredService<DeviceService>();
            // Get all devices
            var devices = db.SmsGateDevices.ToList();
            var deviceDetails = new List<DeviceHealthDetails>();

            await device.SyncDevice(db, smsGate, cloud);
            foreach (var device in devices)
            {
                await SyncDevice(device, db, smsGate);
            }

            await db.SaveChangesAsync();
            // Send pending messages to cloud
            await SendPendingMessages(db, cloud);

        }

        private async Task SyncDevice(SmsGateDevice device, AgentDbContext db, SmsGateService smsGate)
        {
            var syncTime = DateTime.Now;
            int offset = 0;
            const int limit = 100;
            bool syncCompleted = true;
            while (true)
            {
                var json = await smsGate.GetInbox(device, syncTime, limit, offset);
                if (string.IsNullOrEmpty(json))
                {
                    syncCompleted = false;
                    break;
                }


                var messages = ParseMessages(json, device.DeviceUniqueId);
                if (messages.Count == 0)
                    break;

                foreach (var sms in messages)
                {
                    var exists = db.SmsMessages.Any(x => x.SmsGateId == sms.SmsGateId);

                    if (!exists)
                    {
                        db.SmsMessages.Add(sms);
                    }
                }

                if (messages.Count < limit)
                    break;

                offset += limit;
            }

            // update last sync after device finished
            if (syncCompleted)
                device.LastSyncDate = syncTime;
        }



        private async Task SendPendingMessages(AgentDbContext db, CloudService cloud)
        {
            var pending = db.SmsMessages.Where(x => !x.SentToCloud).ToList();
            if (!pending.Any())
                return;

            var deviceIds = pending.Select(i => i.DeviceId).Distinct().ToList();
            var devices = db.SmsGateDevices.Where(i => deviceIds.Contains(i.DeviceUniqueId)).ToList();
            var Params = pending.Select(i =>
            {
                var device = devices.FirstOrDefault(x => x.DeviceUniqueId == i.DeviceId);
                return new SmsMessageRequest
                {
                    SmsGateId = i.SmsGateId,
                    CustomerId = device.CustomerId.Value,
                    BranchId = device.BranchId.Value,
                    Message = i.ContentPreview,
                    DeviceName = i.SimNumber == 1 ? device.Sim1Name : device.Sim2Name,
                    ProviderPhone = i.SimNumber == 1 ? device.Sim1Number : device.Sim2Number,
                    ProviderName = i.Sender,
                    SimNumber = i.SimNumber.ToString(),
                    CreatedAt = i.CreatedAt
                };
            }).ToList();
            var success = await cloud.SendBulkSms(Params);

            if (success)
            {
                db.SmsMessages.RemoveRange(pending);
                await db.SaveChangesAsync();
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
