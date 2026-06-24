using System.Text.Json;

namespace SMSGateWorkerService.ErrorLogs
{
    public static class ErrorLogStore
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private static string FilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "errors.txt");

        public static async Task SaveAsync(SMSGateWorkerService.Models.ErrorLog error)
        {
            try
            {
                await _lock.WaitAsync();
                var json = JsonSerializer.Serialize(error);
                await File.AppendAllTextAsync(FilePath,json + Environment.NewLine);
            }
            catch
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public static async Task RetryAsync(Func<SMSGateWorkerService.Models.ErrorLog, Task<bool>> sender)
        {
            try
            {
                await _lock.WaitAsync();

                if (!File.Exists(FilePath))
                    return;

                var lines = await File.ReadAllLinesAsync(FilePath);
                if (lines.Length == 0)
                    return;

                var remaining = new List<string>();

                foreach (var line in lines)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var error = JsonSerializer.Deserialize<SMSGateWorkerService.Models.ErrorLog>(line);

                        if (error == null)
                            continue;

                        var success = await sender(error);

                        if (!success)
                            remaining.Add(line);
                    }
                    catch
                    {
                        remaining.Add(line);
                    }
                }

                await File.WriteAllLinesAsync(FilePath, remaining);
            }
            catch
            {
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
