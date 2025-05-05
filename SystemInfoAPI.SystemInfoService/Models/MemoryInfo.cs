namespace SystemInfoAPI.SystemInfoService.Models
{
     public class MemoryInfo
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long FreeBytes { get; set; }
        public double UsagePercent { get; set; }
    }
}