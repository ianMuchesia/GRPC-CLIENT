
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace SystemInfoAPI.Client.RestClient
{
     class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("System Info REST Client");
            Console.WriteLine("=======================");


             // Configure services
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information))
                .AddHttpClient("SystemInfoAPI", client =>
                {
                    client.BaseAddress = new Uri("http://localhost:5058/");
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .Services
                .BuildServiceProvider();


            
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory == null)
            {
                throw new InvalidOperationException("ILoggerFactory is not registered in the service provider.");
            }
            var logger = loggerFactory.CreateLogger<Program>();
            
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory == null)
            {
                throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
            }
            var client = httpClientFactory.CreateClient("SystemInfoAPI");


             try
            {
                if (args.Length > 0 && args[0] == "polling")
                {
                    await PollingDemoAsync(client, logger);
                }
                else if (args.Length > 0 && args[0] == "auth")
                {
                    await AuthenticatedCallDemoAsync(client, logger);
                }
                else
                {
                    await BasicCallDemoAsync(client, logger);
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP Request Error: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error");
            }
        }

        
        static async Task BasicCallDemoAsync(HttpClient client, ILogger logger)
        {
            logger.LogInformation("Making REST API call...");
            
            // Make the REST call
            var response = await client.GetAsync("api/SystemInfo");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Response: {Content}", content);
            
            // Parse and display the response
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                
                logger.LogInformation("\nSystem Information (REST):");
                logger.LogInformation("- OS: {OsName} {OsVersion}", 
                    root.GetProperty("osName").GetString(),
                    root.GetProperty("osVersion").GetString());
                
                logger.LogInformation("- CPU Usage: {CpuUsage}%", 
                    root.GetProperty("cpuUsagePercent").GetDouble());
                
                var memInfo = root.GetProperty("memoryInfo");
                logger.LogInformation("- Memory Usage: {MemoryUsage}%", 
                    memInfo.GetProperty("usagePercent").GetDouble());
                
                logger.LogInformation("- Uptime: {Uptime} seconds", 
                    root.GetProperty("uptimeSeconds").GetInt64());
            }
            
            // Individual metrics demo
            logger.LogInformation("\nRequesting individual metrics:");
            
            // Get CPU usage
            response = await client.GetAsync("api/SystemInfo/cpu");
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("CPU Usage: {Content}", content);
            
            // Get memory usage
            response = await client.GetAsync("api/SystemInfo/memory");
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Memory Usage: {Content}", content);
            
            // Get OS info
            response = await client.GetAsync("api/SystemInfo/os");
            response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("OS Info: {Content}", content);
        }


         static async Task PollingDemoAsync(HttpClient client, ILogger logger)
        {
            logger.LogInformation("REST Polling Demo - Simulating streaming behavior with polling");
            logger.LogInformation("Polling system info every second for 10 seconds...\n");
            
            // Create a stopwatch to measure elapsed time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Poll for 10 seconds
            for (int i = 0; i < 10; i++)
            {
                var response = await client.GetAsync("api/SystemInfo");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    var elapsedSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
                    
                    logger.LogInformation("[{Elapsed:F1}s] CPU: {CpuUsage}%, Memory: {MemoryUsage}%, Uptime: {Uptime}s",
                        elapsedSeconds,
                        root.GetProperty("cpuUsagePercent").GetDouble(),
                        root.GetProperty("memoryInfo").GetProperty("usagePercent").GetDouble(),
                        root.GetProperty("uptimeSeconds").GetInt64());
                }
                
                await Task.Delay(1000); // Wait 1 second before polling again
            }
            
            logger.LogInformation("\nPolling demo completed.");
            logger.LogInformation("Note the disadvantages of polling compared to gRPC streaming:");
            logger.LogInformation("- Each update requires a new HTTP request");
            logger.LogInformation("- Higher latency and overhead");
            logger.LogInformation("- Client must implement polling logic");
            logger.LogInformation("- Hard to achieve exact interval timing");
        }

        static async Task AuthenticatedCallDemoAsync(HttpClient client, ILogger logger)
        {
            logger.LogInformation("Authenticated REST call demo...");
            
            // Simulating obtaining a JWT token
            var token = await GetJwtTokenAsync();
            
            // Create a new HTTP request
            var request = new HttpRequestMessage(HttpMethod.Get, "api/SystemInfo");
            
            // Add the authorization header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            try
            {
                // Send the request
                var response = await client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    logger.LogInformation("Authenticated call succeeded!");
                    logger.LogInformation("Response: {Content}", content);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    logger.LogError("Authentication failed: Unauthorized");
                }
                else
                {
                    logger.LogError("Request failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error making authenticated request");
            }
        }
        
        private static async Task<string> GetJwtTokenAsync()
        {
            // In a real app, this would call your auth endpoint to get a JWT token
            // This is just a placeholder
            await Task.Delay(100); // Simulate network call
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJzeXN0ZW1pbmZvY2xpZW50IiwibmFtZSI6IlN5c3RlbSBJbmZvIENsaWVudCIsImlhdCI6MTcxNDkxMzUzMH0.QIRjrQDT5Zx7eBcVzXdTR6KK7UwGRpY5LFcOUAXmTLE";
        }
    }
}