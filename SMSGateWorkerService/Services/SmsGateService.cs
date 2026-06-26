using SMSGateWorkerService.Data;
using SMSGateWorkerService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SMSGateWorkerService.Services
{
    public class SmsGateService
    {
        private readonly HttpClient _client;

        public SmsGateService(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> GetInbox(SmsGateDevice device,DateTime from, DateTime to, int limit = 100, int offset = 0)
        {
            try
            {
                var url =
                    $"{device.BaseUrl}:8080/inbox" +
                    $"?type=SMS" +
                    $"&limit={limit}" +
                    $"&offset={offset}" +
                    $"&from={Uri.EscapeDataString(from.ToString("yyyy-MM-ddTHH:mm:ssZ"))}" +
                    $"&to={Uri.EscapeDataString(to.ToString("yyyy-MM-ddTHH:mm:ssZ"))}" +
                    $"&deviceId={device.DeviceUniqueId}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{device.Username}:{device.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _client.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<DeviceHealthResult>> GetDeviceHealth(List<SmsGateDevice> devices)
        {
            const int maxParallelRequests = 10;
            using var semaphore = new SemaphoreSlim(maxParallelRequests);

            try
            {
                var tasks = devices.Select(async device =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        var url = $"{device.BaseUrl}:8080/health";
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{device.Username}:{device.Password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                        //using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync();
                        var health = JsonSerializer.Deserialize<SystemPing>(json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (health == null)
                            return null;

                        return new DeviceHealthResult
                        {
                            DeviceId = device.DeviceUniqueId,
                            Health = health
                        };
                    }
                    catch
                    {
                        return new DeviceHealthResult
                        {
                            DeviceId = device.DeviceUniqueId,
                            Health = null
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                return (await Task.WhenAll(tasks)).Where(x => x != null && x.Health != null).ToList();
            }
            catch
            {
                return new List<DeviceHealthResult>();
            }
        }

        public async Task<List<DeviceDetails>> GetDevice(List<SmsGateDevice> devices)
        {
            const int maxParallelRequests = 10;
            using var semaphore = new SemaphoreSlim(maxParallelRequests);

            try
            {
                var tasks = devices.Select(async device =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        var url = $"{device.BaseUrl}:8080/device";
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{device.Username}:{device.Password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _client.SendAsync(request, cts.Token);
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<List<DeviceDetails>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return result?.FirstOrDefault();
                    }
                    catch
                    {
                        return null;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                return (await Task.WhenAll(tasks)).Where(x => x != null).ToList();
            }
            catch
            {
                return new List<DeviceDetails>();
            }
        }

        public async Task<string> RefreshInbox(SmsGateDevice device, DateTime since, DateTime until)
        {
            try
            {
                var url = $"{device.BaseUrl}:8080/inbox/refresh";
                var body = new
                {
                    deviceId = device.DeviceId,
                    since = since.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    until = until.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonSerializer.Serialize(body);
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{device.Username}:{device.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _client.SendAsync(request, cts.Token);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
