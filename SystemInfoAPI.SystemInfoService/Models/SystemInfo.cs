namespace SystemInfoAPI.SystemInfoService.Models
{

     public class SystemInfo
    {
        public string OsName { get; set; }
        public string OsVersion { get; set; }
        public double CpuUsagePercent { get; set; }
        public MemoryInfo MemoryInfo { get; set; }
        public long UptimeSeconds { get; set; }
        public long Timestamp { get; set; }
    }
}