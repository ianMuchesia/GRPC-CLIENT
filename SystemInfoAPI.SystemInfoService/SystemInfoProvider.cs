


using System.Diagnostics;
using System.Runtime.InteropServices;
using SystemInfoAPI.SystemInfoService.Models;

namespace SystemInfoAPI.SystemInfoService
{
    public class SystemInfoProvider
    {
        private readonly Process _process;
        private readonly PerformanceCounter _cpuCounter;

        public SystemInfoProvider()
        {
            _process = Process.GetCurrentProcess();

            // Initialize CPU counter if on Windows platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // First call will always return 0
            }
        }

         public async Task<SystemInfo> GetSystemInfoAsync()
        {
            // Allow a small delay to get proper CPU reading
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Task.Delay(100);
            }

            return new SystemInfo
            {
                OsName = RuntimeInformation.OSDescription,
                OsVersion = Environment.OSVersion.VersionString,
                CpuUsagePercent = GetCpuUsage(),
                MemoryInfo = GetMemoryInfo(),
                UptimeSeconds = GetSystemUptime(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

         private double GetCpuUsage()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Math.Round(_cpuCounter.NextValue(), 2);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // On Linux, read from /proc/stat
                    // Simplified implementation - for production code, use a more robust approach
                    return Math.Round(new Random().NextDouble() * 100, 2); // Mock implementation
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // On macOS, use system utilities
                    // Simplified implementation - for production code, use a more robust approach
                    return Math.Round(new Random().NextDouble() * 100, 2); // Mock implementation
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        
        private MemoryInfo GetMemoryInfo()
        {
            try
            {
                var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                var usedMemory = Process.GetProcesses().Sum(p => {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                });
                var freeMemory = totalMemory - usedMemory;
                var usagePercent = (double)usedMemory / totalMemory * 100;

                return new MemoryInfo
                {
                    TotalBytes = totalMemory,
                    UsedBytes = usedMemory,
                    FreeBytes = freeMemory,
                    UsagePercent = Math.Round(usagePercent, 2)
                };
            }
            catch
            {
                // Fallback to process memory if global memory information is not available
                var process = Process.GetCurrentProcess();
                return new MemoryInfo
                {
                    TotalBytes = Environment.WorkingSet,
                    UsedBytes = process.WorkingSet64,
                    FreeBytes = Environment.WorkingSet - process.WorkingSet64,
                    UsagePercent = 0
                };
            }
        }

          private long GetSystemUptime()
        {
            try
            {
                return (long)TimeSpan.FromMilliseconds(Environment.TickCount64).TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}