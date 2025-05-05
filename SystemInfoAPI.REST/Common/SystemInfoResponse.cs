namespace SystemInfoAPI.REST.Common
{
     public class SystemInfoResponse
    {
        public string OsName { get; set; }
        public string OsVersion { get; set; }
        public double CpuUsagePercent { get; set; }
        public MemoryInfoResponse MemoryInfo { get; set; }
        public long UptimeSeconds { get; set; }
        public long Timestamp { get; set; }
    }
}