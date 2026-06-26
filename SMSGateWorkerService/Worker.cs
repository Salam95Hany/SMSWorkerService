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
            using (var scope = _serviceProvider.CreateScope())
            {
                var connectivity =scope.ServiceProvider.GetRequiredService<ConnectivityService>();
                await connectivity.WaitForServerAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    using var scope = _serviceProvider.CreateScope();
                    var connectivity = scope.ServiceProvider.GetRequiredService<ConnectivityService>();
                    if (!await connectivity.IsServerAvailable())
                    {
                        await Task.Delay(interval, stoppingToken);
                        continue;
                    }

                    await Sync(scope.ServiceProvider);

                    if (DateTime.UtcNow - _lastRetryTime >= TimeSpan.FromHours(2))
                    {
                        try
                        {
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
                        using var errorScope = _serviceProvider.CreateScope();
                        var cloud = errorScope.ServiceProvider.GetRequiredService<CloudService>();
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

        private async Task Sync(IServiceProvider provider)
        {
            var _context = provider.GetRequiredService<AgentDbContext>();
            var smsGate = provider.GetRequiredService<SmsGateService>();
            var cloud = provider.GetRequiredService<CloudService>();

            var device = provider.GetRequiredService<DeviceService>();
            var inbox = provider.GetRequiredService<InboxService>();
            var sendsms = provider.GetRequiredService<SendMessageService>();

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
    }
}
