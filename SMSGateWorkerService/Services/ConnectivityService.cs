using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSGateWorkerService.Services
{
    public class ConnectivityService
    {
        private readonly HttpClient _client;

        public ConnectivityService(HttpClient client)
        {
            _client = client;
        }

        public async Task<bool> IsServerAvailable()
        {
            try
            {
                using var response = await _client.GetAsync("/");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task WaitForServerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var response = await _client.GetAsync("/", token);
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
