namespace SystemInfoAPI.REST.Common
{
    public class MemoryInfoResponse
    {
        public long TotalBytes { get; set; }
        public long UsedBytes { get; set; }
        public long FreeBytes { get; set; }
        public double UsagePercent { get; set; }
    }
}