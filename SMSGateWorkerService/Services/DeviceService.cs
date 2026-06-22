using Microsoft.EntityFrameworkCore;
using SMSGateWorkerService.Data;
using SMSGateWorkerService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            var LatestVersion = _context.DeviceSyncVersions.FirstOrDefault();
            var DevicesRes = await cloud.GetAllDevices(CustomerId, BranchId, LatestVersion.CurrentVersion);
            if (DevicesRes == null)
                return;

            var LocalDevices = await _context.SmsGateDevices.Where(i => i.IsActive).ToListAsync();
            var Devices = ParseDevices(DevicesRes);
            if (LatestVersion.CurrentVersion == 0 || Devices.Changes.Any(i => i.Action == ChangeLogAction.Created))
            {
                await _context.SmsGateDevices.AddRangeAsync(Devices.Devices);
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

            LatestVersion.CurrentVersion = Devices.LatestVersion;
            await _context.SaveChangesAsync();
        }

        public async Task SyncDeviceDetails(AgentDbContext _context, SmsGateService smsGate, CloudService cloudService)
        {
            var LocalDevices = await _context.SmsGateDevices.Where(i => i.IsActive).ToListAsync();
            var SystemPings = await smsGate.GetDeviceHealth(LocalDevices);
            if (SystemPings != null && SystemPings.Count > 0)
            {
                var deviceDetails = ParseSystemPing(SystemPings);
                await cloudService.SendBulkDeviceDetails(deviceDetails);
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

        private List<DeviceHealthDetails> ParseSystemPing(List<DeviceHealthResult> Model)
        {

            return Model.Select(i => new DeviceHealthDetails
            {
                DeviceId = i.DeviceId,
                MessagesFailed = GetValue(i.Health, "messages:failed"),
                ConnectionStatus = GetValue(i.Health, "connection:status"),
                ConnectionTransport = GetValue(i.Health, "connection:transport"),
                BatteryLevel = GetValue(i.Health, "battery:level"),
                BatteryCharging = GetValue(i.Health, "battery:charging") != 0
            }).ToList();
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
