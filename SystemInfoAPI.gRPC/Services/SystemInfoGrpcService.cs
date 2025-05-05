using Grpc.Core;
using SystemInfoAPI.SystemInfoService;
using SystemInfoAPI.SystemInfoService.Models;

namespace SystemInfoAPI.gRPC.Services
{
    public class SystemInfoGrpcService : SystemInfoService.SystemInfoServiceBase
    {
        private readonly SystemInfoProvider _systemInfoProvider;
        private readonly ILogger<SystemInfoGrpcService> _logger;

        public SystemInfoGrpcService(
          SystemInfoProvider systemInfoProvider,
          ILogger<SystemInfoGrpcService> logger)
        {
            _systemInfoProvider = systemInfoProvider;
            _logger = logger;
        }


        public override async Task<SystemInfoResponse> GetSystemInfo(
               SystemInfoRequest request,
               ServerCallContext context)
        {
            _logger.LogInformation("Processing gRPC request for system information");

            try
            {
                var sysInfo = await _systemInfoProvider.GetSystemInfoAsync();

                return MapToGrpcResponse(sysInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system information");
                throw new RpcException(new Status(StatusCode.Internal, "Failed to retrieve system information"));
            }
        }

        public override async Task StreamSystemInfo(
         SystemInfoRequest request,
         IServerStreamWriter<SystemInfoResponse> responseStream,
         ServerCallContext context)
        {
            var intervalMs = request.UpdateIntervalMs > 0 ? request.UpdateIntervalMs : 1000;
            _logger.LogInformation($"Starting system info stream with interval: {intervalMs}ms");

            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var sysInfo = await _systemInfoProvider.GetSystemInfoAsync();
                    await responseStream.WriteAsync(MapToGrpcResponse(sysInfo));
                    await Task.Delay(intervalMs, context.CancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Stream was canceled by the client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system info streaming");
                throw new RpcException(new Status(StatusCode.Internal, "Error occurred during streaming"));
            }
        }
        private SystemInfoResponse MapToGrpcResponse(SystemInfo sysInfo)
        {
            return new SystemInfoResponse
            {
                OsName = sysInfo.OsName,
                OsVersion = sysInfo.OsVersion,
                CpuUsagePercent = sysInfo.CpuUsagePercent,
                MemoryInfo = new MemoryInfo
                {
                    TotalBytes = sysInfo.MemoryInfo.TotalBytes,
                    UsedBytes = sysInfo.MemoryInfo.UsedBytes,
                    FreeBytes = sysInfo.MemoryInfo.FreeBytes,
                    UsagePercent = sysInfo.MemoryInfo.UsagePercent
                },
                UptimeSeconds = sysInfo.UptimeSeconds,
                Timestamp = sysInfo.Timestamp
            };
        }
    }




}