using SMSGateWorkerService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

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
            if (messages == null || !messages.Any())
                return true;

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/SMSReader/webhook");
            request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
            request.Content = JsonContent.Create(messages);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendRefreshBulkSms(List<SmsMessageRequest> messages)
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

        public async Task<bool> SendBulkDeviceDetails(List<DeviceHealthDetails> details)
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
                return null;
            }
            
        }

        public async Task<string> GetDeviceRefreshInboxSetting(Guid CustomerId, int BranchId)
        {
            var url = $"/api/Worker/inbox" + $"?CustomerId={CustomerId}" + $"&BranchId={BranchId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> UpdateDeviceRefreshInboxStatus(Guid CustomerId, int BranchId, string DeviceId)
        {
            var url = $"/api/Worker/inbox/status" + $"?CustomerId={CustomerId}" + $"&BranchId={BranchId}" + $"&DeviceId={DeviceId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Secret_Key", "f8e8d0d6b0a14f7c9c0f6c9d5f3a8e4b");
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
