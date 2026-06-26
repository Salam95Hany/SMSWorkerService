using SMSGateWorkerService.Data;
using SMSGateWorkerService.ErrorLogs;
using SMSGateWorkerService.Services;

namespace SMSGateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private DateTime _lastRetryTime = DateTime.MinValue;
        public Worker(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(_configuration.GetValue<int>("Sync:IntervalSeconds"));

            await WaitForServerAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Sync();

                    if (DateTime.UtcNow - _lastRetryTime >= TimeSpan.FromHours(2))
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var cloud = scope.ServiceProvider.GetRequiredService<CloudService>();
                            await ErrorLogStore.RetryAsync(async error => await cloud.SendLogException(error));
                            _lastRetryTime = DateTime.UtcNow;
                        }
                        catch
                        {
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var cloud = scope.ServiceProvider.GetRequiredService<CloudService>();
                        await cloud.SendLogException(CreateErrorLog(ex));
                    }
                    catch
                    {
                    }
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task Sync()
        {
            using var scope = _serviceProvider.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
            var smsGate = scope.ServiceProvider.GetRequiredService<SmsGateService>();
            var cloud = scope.ServiceProvider.GetRequiredService<CloudService>();

            var device = scope.ServiceProvider.GetRequiredService<DeviceService>();
            var inbox = scope.ServiceProvider.GetRequiredService<InboxService>();
            var sendsms = scope.ServiceProvider.GetRequiredService<SendMessageService>();

            await device.SyncDevice(_context, smsGate, cloud);
            await sendsms.SyncSendMessages(_context, smsGate, cloud);
            await inbox.RefreshDeviceInbox(_context, smsGate, cloud);
        }

        private SMSGateWorkerService.Models.ErrorLog CreateErrorLog(Exception ex)
        {
            return new SMSGateWorkerService.Models.ErrorLog
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Service = ex.Data["Service"]?.ToString(),
                Method = ex.Data["Method"]?.ToString(),
                DeviceId = ex.Data["DeviceId"]?.ToString(),
                CustomerId = Guid.TryParse(ex.Data["CustomerId"]?.ToString(), out var customerId) ? customerId : Guid.Empty,
                BranchId = int.TryParse(ex.Data["BranchId"]?.ToString(), out var branchId) ? branchId : 0
            };
        }


        private async Task WaitForServerAsync(CancellationToken token)
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var response = await client.GetAsync("https://smsreader-api.runasp.net/", token);
                    return;
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }
    }
}
