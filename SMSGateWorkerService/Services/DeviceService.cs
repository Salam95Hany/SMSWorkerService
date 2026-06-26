using Microsoft.EntityFrameworkCore;
using SMSGateWorkerService.Data;
using SMSGateWorkerService.ErrorLog;
using SMSGateWorkerService.Models;
using System.Text.Json;

namespace SMSGateWorkerService.Services
{
    public class DeviceService
    {
        private readonly IConfiguration _configuration;
        Guid CustomerId;
        int BranchId;
        public DeviceService(IConfiguration configuration)
        {
            _configuration = configuration;
            CustomerId = _configuration.GetValue<Guid>("CustomerId");
            BranchId = _configuration.GetValue<int>("BranchId");
        }

        public async Task SyncDevice(AgentDbContext _context, SmsGateService smsGate, CloudService cloud)
        {
            await GetDevices(_context, cloud);
            await SyncDeviceDetails(_context, smsGate, cloud);
        }
        public async Task GetDevices(AgentDbContext _context, CloudService cloud)
        {
            try
            {
                var LatestVersion = _context.DeviceSyncVersions.FirstOrDefault();
                var DevicesRes = await cloud.GetAllDevices(CustomerId, BranchId, LatestVersion.CurrentVersion);
                if (DevicesRes == null)
                    return;

                var LocalDevices = await _context.SmsGateDevices.Where(i => i.IsActive).ToListAsync();
                var Devices = ParseDevices(DevicesRes);
                if (LatestVersion.CurrentVersion == 0 || Devices.Devices.Count > 0)
                {
                    var NewDevices = Devices.Devices.Where(d => !LocalDevices.Any(ld => ld.DeviceUniqueId == d.DeviceUniqueId)).ToList();
                    NewDevices.ForEach(i => i.LastSyncDate = DateTime.Now);
                    if (NewDevices.Any())
                        await _context.SmsGateDevices.AddRangeAsync(NewDevices);
                }

                if (Devices.Changes.Any(i => i.Action == ChangeLogAction.Deleted))
                {
                    var Deleted = Devices.Changes.Where(i => i.Action == ChangeLogAction.Deleted).ToList();
                    foreach (var item in Deleted)
                    {
                        var DelDevice = LocalDevices.FirstOrDefault(i => i.DeviceUniqueId == item.DeviceId);
                        if (DelDevice != null)
                            DelDevice.IsActive = bool.Parse(item.FieldValue);
                    }
                }

                if (Devices.Changes.Any(i => i.Action == ChangeLogAction.Updated))
                {
                    var Updated = Devices.Changes.Where(i => i.Action == ChangeLogAction.Updated).ToList();
                    var properties = typeof(SmsGateDevice).GetProperties().ToDictionary(x => x.Name, x => x);

                    foreach (var item in Updated)
                    {
                        var localDevice = LocalDevices.FirstOrDefault(i => i.DeviceUniqueId == item.DeviceId);

                        if (localDevice == null)
                            continue;

                        if (!properties.TryGetValue(item.FieldKey, out var property))
                            continue;

                        if (!property.CanWrite)
                            continue;

                        var value = ConvertValue(item.FieldValue, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                        property.SetValue(localDevice, value);
                    }
                }

                if (Devices.Devices.Count > 0 || Devices.Changes.Count > 0)
                {
                    LatestVersion.CurrentVersion = Devices.LatestVersion;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ex.AddException("DeviceService", "SyncDeviceDetails", CustomerId, BranchId);
                throw;
            }
        }

        public async Task SyncDeviceDetails(AgentDbContext _context, SmsGateService smsGate, CloudService cloudService)
        {
            try
            {
                var LocalDevices = await _context.SmsGateDevices.Where(i => i.IsActive).ToListAsync();
                var SystemPingsTask = smsGate.GetDeviceHealth(LocalDevices);
                var DevicesLastSeenTask = smsGate.GetDevice(LocalDevices);
                await Task.WhenAll(SystemPingsTask, DevicesLastSeenTask);
                if (SystemPingsTask.Result != null && SystemPingsTask.Result.Count > 0)
                {
                    var deviceDetails = ParseSystemPing(SystemPingsTask.Result, LocalDevices, DevicesLastSeenTask.Result);
                    await cloudService.SendBulkDeviceDetails(deviceDetails);
                }
            }
            catch (Exception ex)
            {
                ex.AddException("DeviceService", "SyncDeviceDetails", CustomerId, BranchId);
                throw;
            }

        }

        private DeviceWorkerResponse ParseDevices(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<DeviceWorkerResponse>(json, options);

            if (response == null)
                return new DeviceWorkerResponse();

            return response;
        }

        private List<DeviceHealthDetails> ParseSystemPing(List<DeviceHealthResult> model, List<SmsGateDevice> devices, List<DeviceDetails> lastSeenDevices)
        {
            var devicesLookup = devices.ToDictionary(x => x.DeviceUniqueId);
            var lastSeenLookup = lastSeenDevices.ToDictionary(x => x.Id);
            var result = new List<DeviceHealthDetails>(model.Count);

            foreach (var item in model)
            {
                devicesLookup.TryGetValue(item.DeviceId, out var device);
                lastSeenLookup.TryGetValue(item.DeviceId, out var lastSeenDevice);

                if (device == null || lastSeenDevice?.LastSeen == null)
                    continue;

                result.Add(new DeviceHealthDetails
                {
                    DeviceId = item.DeviceId,
                    CustomerId = device.CustomerId,
                    BranchId = device.BranchId,
                    DeviceName = device.DeviceName,
                    MessagesFailed = GetValue(item.Health, "messages:failed"),
                    ConnectionStatus = GetValue(item.Health, "connection:status"),
                    ConnectionTransport = GetValue(item.Health, "connection:transport"),
                    BatteryLevel = GetValue(item.Health, "battery:level"),
                    BatteryCharging = GetValue(item.Health, "battery:charging") != 0,
                    LastSeen = lastSeenDevice.LastSeen,
                    LastSyncDate = device.LastSyncDate
                });
            }

            return result;
        }


        private int GetValue(SystemPing systemPing, string key)
        {
            if (systemPing?.Checks != null && systemPing.Checks.TryGetValue(key, out var healthCheck))
            {
                return healthCheck.ObservedValue;
            }

            return 0;
        }

        private object ConvertValue(string value, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsEnum)
                return Enum.Parse(type, value);

            if (type == typeof(Guid))
                return Guid.Parse(value);

            return Convert.ChangeType(value, type);
        }
    }
}
