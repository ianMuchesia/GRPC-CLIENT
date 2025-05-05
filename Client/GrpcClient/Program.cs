using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Grpc.Net.Client;
using Grpc.Core;
using SystemInfoAPI.gRPC;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SystemInfoAPI.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("System Info gRPC Client");
            Console.WriteLine("========================");

            // Configure logging
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory == null)
            {
                throw new InvalidOperationException("ILoggerFactory service is not available.");
            }
            var logger = loggerFactory.CreateLogger<Program>();

            // Setup gRPC channel with TLS
            var channel = GrpcChannel.ForAddress("http://localhost:5001", new GrpcChannelOptions
            {
                LoggerFactory = loggerFactory
            });

            // Create client - using the namespace generated from the proto file
            var client = new SystemInfoService.SystemInfoServiceClient(channel);

            try
            {
                if (args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "stream":
                            await StreamSystemInfoDemo(client, logger);
                            break;
                        case "unary":
                            await UnarySystemInfoDemo(client, logger);
                            break;
                        default:
                            await CompareGrpcAndRestDemo(client, logger);
                            break;
                    }
                }
                else
                {
                    await CompareGrpcAndRestDemo(client, logger);
                }
            }
            catch (RpcException ex)
            {
                logger.LogError($"RPC Error: {ex.StatusCode} - {ex.Message}");
                logger.LogError($"Details: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                await channel.ShutdownAsync();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task UnarySystemInfoDemo(SystemInfoService.SystemInfoServiceClient client, ILogger logger)
        {
            logger.LogInformation("Making unary gRPC call to get system information...");
            
            // Create the request
            var request = new SystemInfoRequest();
            
            // Make the call
            var response = await client.GetSystemInfoAsync(request);
            
            // Display the results
            DisplaySystemInfo(response, logger);
        }

        static void DisplaySystemInfo(SystemInfoResponse info, ILogger logger)
        {
            logger.LogInformation($"System Information (collected at {DateTimeOffset.FromUnixTimeSeconds(info.Timestamp)})");
            logger.LogInformation($"  OS: {info.OsName} {info.OsVersion}");
           
            logger.LogInformation($"  Memory: {info.MemoryInfo.UsagePercent:F1}% ({FormatBytes(info.MemoryInfo.UsedBytes)} used of {FormatBytes(info.MemoryInfo.TotalBytes)})");
            logger.LogInformation($"  Uptime: {TimeSpan.FromSeconds(info.UptimeSeconds)}");
            
            // if (info.DiskInfo.Count > 0)
            // {
            //     logger.LogInformation("  Disks:");
            //     foreach (var disk in info.DiskInfo)
            //     {
            //         logger.LogInformation($"    {disk.Name} ({disk.MountPoint}): {disk.UsagePercent:F1}% used, {FormatBytes(disk.FreeBytes)} free");
            //     }
            // }
        }

        static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            
            return $"{size:F2} {sizes[order]}";
        }

        static async Task StreamSystemInfoDemo(SystemInfoService.SystemInfoServiceClient client, ILogger logger)
        {
            logger.LogInformation("Starting gRPC streaming demo...");
            logger.LogInformation("Receiving system info updates every second for 30 seconds...\n");
            
            // Create a cancellation token that will cancel after 30 seconds
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            try
            {
                // Setup streaming call with update interval of 1000ms
                var request = new SystemInfoRequest { UpdateIntervalMs = 1000 };
                using var streamingCall = client.StreamSystemInfo(request, cancellationToken: cts.Token);
                
                // Create a stopwatch to measure elapsed time
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Process streaming responses
                await foreach (var update in streamingCall.ResponseStream.ReadAllAsync(cts.Token))
                {
                    var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
                    
                    logger.LogInformation($"[{elapsedSeconds:F1}s] CPU: {update.CpuUsagePercent:F1}%, " +
                        $"Memory: {update.MemoryInfo.UsagePercent:F1}%, " +
                        $"Uptime: {update.UptimeSeconds}s");
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                logger.LogInformation("\nStream was cancelled after 30 seconds as expected.");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("\nStream was cancelled after 30 seconds as expected.");
            }
            
            logger.LogInformation("\nStreaming demo completed.");
            logger.LogInformation("Advantages of streaming:");
            logger.LogInformation("- Single connection for multiple data points");
            logger.LogInformation("- Server pushes updates (no polling needed)");
            logger.LogInformation("- Lower latency for real-time monitoring");
            logger.LogInformation("- Reduced overhead compared to repeated REST calls");
        }

        static async Task CompareGrpcAndRestDemo(SystemInfoService.SystemInfoServiceClient client, ILogger logger)
        {
            // First call the gRPC endpoint
            logger.LogInformation("Performing basic gRPC call...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Make the unary RPC call
            var reply = await client.GetSystemInfoAsync(new SystemInfoRequest());
            var grpcTime = stopwatch.ElapsedMilliseconds;
            
            logger.LogInformation($"gRPC call completed in {grpcTime} ms");
            logger.LogInformation("System Information (gRPC):");
            logger.LogInformation($"- OS: {reply.OsName} {reply.OsVersion}");
            logger.LogInformation($"- CPU Usage: {reply.CpuUsagePercent:F1}%");
            // logger.LogInformation($"- Memory Usage: {reply.MemoryInfo.UsagePercent:F1}%");
            logger.LogInformation($"- Uptime: {reply.UptimeSeconds} seconds");
            
            // Now make the REST call to compare
            logger.LogInformation("\nPerforming equivalent REST call...");
            
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                try
                {
                    stopwatch.Restart();
                    var response = await httpClient.GetAsync("http://localhost:5058/api/SystemInfo");
                    response.EnsureSuccessStatusCode();
                    
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var restTime = stopwatch.ElapsedMilliseconds;
                    
                    logger.LogInformation($"REST call completed in {restTime} ms");
                    logger.LogInformation($"REST Response: {jsonResponse}");
                    
                    logger.LogInformation("\nPerformance Comparison:");
                    logger.LogInformation($"- gRPC: {grpcTime} ms");
                    logger.LogInformation($"- REST: {restTime} ms");
                    
                    logger.LogInformation("\nComparison Notes:");
                    logger.LogInformation("- gRPC uses binary protocol (more efficient)");
                    logger.LogInformation("- REST uses JSON (human readable)");
                    logger.LogInformation("- gRPC has strongly typed contracts");
                    logger.LogInformation("- REST requires serialization/deserialization");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to make REST call: {ex.Message}");
                }
            }
        }
    }
}