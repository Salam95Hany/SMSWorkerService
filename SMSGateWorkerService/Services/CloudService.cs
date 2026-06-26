using SMSGateWorkerService.ErrorLog;
using SMSGateWorkerService.ErrorLogs;
using SMSGateWorkerService.Models;
using System.Net.Http.Json;

namespace SMSGateWorkerService.Services
{
    public class CloudService
    {
        private readonly HttpClient _client;

        public CloudService(HttpClient client, IConfiguration config)
        {
            _client = client;
            _client.BaseAddress = new Uri(config["Cloud:BaseUrl"]);
        }

        public async Task<bool> SendBulkSms(List<SmsMessageRequest> messages)
        {
            try
            {
                if (messages == null || !messages.Any())
                    return true;

                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/SMSReader/webhook");
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                request.Content = JsonContent.Create(messages);
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "SendBulkSms", messages.FirstOrDefault().CustomerId, messages.FirstOrDefault().BranchId);
                throw;
            }
        }

        public async Task<bool> SendRefreshBulkSms(List<SmsMessageRequest> messages)
        {
            try
            {
                if (messages == null || !messages.Any())
                    return true;

                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/SMSReader/refresh");
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                request.Content = JsonContent.Create(messages);
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "SendRefreshBulkSms", messages.FirstOrDefault().CustomerId, messages.FirstOrDefault().BranchId);
                throw;
            }
        }

        public async Task<bool> SendBulkDeviceDetails(List<DeviceHealthDetails> details)
        {
            try
            {
                if (details == null || !details.Any())
                    return true;

                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Worker/health");
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                request.Content = JsonContent.Create(details);
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "SendBulkDeviceDetails", details.FirstOrDefault().CustomerId, details.FirstOrDefault().BranchId);
                throw;
            }

        }

        public async Task<string> GetAllDevices(Guid CustomerId, int BranchId, int Version)
        {
            try
            {
                var url = $"/api/Worker/device" + $"?CustomerId={CustomerId}" + $"&BranchId={BranchId}" + $"&Version={Version}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "GetAllDevices", CustomerId, BranchId);
                throw;
            }

        }

        public async Task<string> GetDeviceRefreshInboxSetting(Guid CustomerId, int BranchId)
        {
            try
            {
                var url = $"/api/Worker/inbox" + $"?CustomerId={CustomerId}" + $"&BranchId={BranchId}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "GetDeviceRefreshInboxSetting", CustomerId, BranchId);
                throw;
            }

        }

        public async Task<string> UpdateDeviceRefreshInboxStatus(Guid CustomerId, int BranchId, string DeviceId)
        {
            try
            {
                var url = $"/api/Worker/inbox/status" + $"?CustomerId={CustomerId}" + $"&BranchId={BranchId}" + $"&DeviceId={DeviceId}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ex.AddException("CloudService", "UpdateDeviceRefreshInboxStatus", CustomerId, BranchId, DeviceId);
                throw;
            }
        }

        public async Task<bool> SendLogException(SMSGateWorkerService.Models.ErrorLog error)
        {
            try
            {
                using var request =new HttpRequestMessage(HttpMethod.Post,"/api/Worker/log");
                request.Headers.Add("Secret_Key","f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
                request.Content = JsonContent.Create(error);
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                await ErrorLogStore.SaveAsync(error);
                return false;
            }
        }
    }
}
