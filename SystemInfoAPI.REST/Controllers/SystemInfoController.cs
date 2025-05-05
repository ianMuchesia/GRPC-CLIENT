


using Microsoft.AspNetCore.Mvc;
using SystemInfoAPI.REST.Common;
using SystemInfoAPI.SystemInfoService;

namespace SystemInfoAPI.REST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class SystemInfoController : ControllerBase
    {
         private readonly SystemInfoProvider _systemInfoProvider;

        public SystemInfoController(SystemInfoProvider systemInfoProvider)
        {
            _systemInfoProvider = systemInfoProvider;
        }

        /// <summary>
        /// Gets the current system information
        /// </summary>
        /// <returns>System information including OS, CPU usage, memory usage, and uptime</returns>
        [HttpGet]
        [ProducesResponseType(typeof(SystemInfoResponse), 200)]
        public async Task<IActionResult> GetSystemInfo()
        {
            var sysInfo = await _systemInfoProvider.GetSystemInfoAsync();
            
            var response = new SystemInfoResponse
            {
                OsName = sysInfo.OsName,
                OsVersion = sysInfo.OsVersion,
                CpuUsagePercent = sysInfo.CpuUsagePercent,
                MemoryInfo = new MemoryInfoResponse
                {
                    TotalBytes = sysInfo.MemoryInfo.TotalBytes,
                    UsedBytes = sysInfo.MemoryInfo.UsedBytes,
                    FreeBytes = sysInfo.MemoryInfo.FreeBytes,
                    UsagePercent = sysInfo.MemoryInfo.UsagePercent
                },
                UptimeSeconds = sysInfo.UptimeSeconds,
                Timestamp = sysInfo.Timestamp
            };
            
            return Ok(response);
        }



        /// <summary>
        /// Gets specific system information
        /// </summary>
        /// <param name="metric">The specific metric to retrieve (cpu, memory, os, uptime)</param>
        /// <returns>Specific system information based on the requested metric</returns>
        [HttpGet("{metric}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSystemMetric(string metric)
        {
            var sysInfo = await _systemInfoProvider.GetSystemInfoAsync();
            
            return metric.ToLower() switch
            {
                "cpu" => Ok(new { Usage = sysInfo.CpuUsagePercent }),
                "memory" => Ok(new
                {
                    Total = sysInfo.MemoryInfo.TotalBytes,
                    Used = sysInfo.MemoryInfo.UsedBytes,
                    Free = sysInfo.MemoryInfo.FreeBytes,
                    UsagePercent = sysInfo.MemoryInfo.UsagePercent
                }),
                "os" => Ok(new { Name = sysInfo.OsName, Version = sysInfo.OsVersion }),
                "uptime" => Ok(new { Seconds = sysInfo.UptimeSeconds }),
                _ => NotFound(new { Error = $"Metric '{metric}' not found" })
            };
        }
    }
}