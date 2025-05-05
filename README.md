# System Info API (REST + gRPC)

This project provides system information through both REST and gRPC APIs. It exposes information such as OS details, CPU usage, memory usage, and system uptime.

## Project Structure

```
SystemInfoAPI/
├── SystemInfoService/         # Shared service to get system info
│   └── SystemInfoProvider.cs
├── REST/                      # ASP.NET Core REST API
│   ├── Controllers/
│   │   └── SystemInfoController.cs
│   └── Program.cs
├── gRPC/                      # ASP.NET Core gRPC server
│   ├── Services/
│   │   └── SystemInfoGrpcService.cs
│   ├── Protos/
│   │   └── systeminfo.proto
│   └── Program.cs
├── README.md
```

## Features

- **Shared System Info Provider**: Common service used by both APIs to gather system information
- **REST API**: HTTP-based API endpoints for system information
- **gRPC API**: High-performance RPC framework with streaming capabilities
- **Cross-platform**: Works on Windows, Linux, and macOS

## Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

## Getting Started

### Building the Project

```bash
# Build the entire solution
dotnet build

# Or build individual projects
dotnet build REST/
dotnet build gRPC/
```

### Running the REST API

```bash
cd REST/
dotnet run
```

The REST API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger

### Running the gRPC API

```bash
cd gRPC/
dotnet run
```

The gRPC API will be available at:
- HTTP/2: http://localhost:5001
- HTTP/2 with TLS: https://localhost:5002

## API Usage

### REST API Endpoints

- `GET /api/SystemInfo` - Get all system information
- `GET /api/SystemInfo/os` - Get OS information
- `GET /api/SystemInfo/cpu` - Get CPU usage
- `GET /api/SystemInfo/memory` - Get memory information
- `GET /api/SystemInfo/uptime` - Get system uptime

### gRPC API Methods

- `GetSystemInfo` - Get current system information (unary RPC)
- `StreamSystemInfo` - Stream system information updates (server streaming RPC)

## Using the gRPC Client

### C# Example

```csharp
using Grpc.Net.Client;
using SystemInfoAPI.gRPC;

using var channel = GrpcChannel.ForAddress("https://localhost:5002");
var client = new SystemInfoService.SystemInfoServiceClient(channel);

// Unary call
var reply = await client.GetSystemInfoAsync(new SystemInfoRequest());
Console.WriteLine($"OS: {reply.OsName}, CPU: {reply.CpuUsagePercent}%");

// Streaming call
using var streamingCall = client.StreamSystemInfo(new SystemInfoRequest { UpdateIntervalMs = 1000 });
await foreach (var update in streamingCall.ResponseStream.ReadAllAsync())
{
    Console.WriteLine($"[{DateTimeOffset.FromUnixTimeSeconds(update.Timestamp)}] " +
                      $"CPU: {update.CpuUsagePercent}%, " +
                      $"Memory: {update.MemoryInfo.UsagePercent}%");
}
```

### Python Example

```python
import grpc
import systeminfo_pb2
import systeminfo_pb2_grpc

channel = grpc.insecure_channel('localhost:5001')
stub = systeminfo_pb2_grpc.SystemInfoServiceStub(channel)

# Unary call
response = stub.GetSystemInfo(systeminfo_pb2.SystemInfoRequest())
print(f"OS: {response.os_name}, CPU: {response.cpu_usage_percent}%")

# Streaming call
request = systeminfo_pb2.SystemInfoRequest(update_interval_ms=1000)
for update in stub.StreamSystemInfo(request):
    print(f"CPU: {update.cpu_usage_percent}%, Memory: {update.memory_info.usage_percent}%")
```

## Extending the Project

- Add authentication and authorization
- Implement metrics collection with Prometheus
- Add distributed tracing with OpenTelemetry
- Create a client application to display system information in real-time

## License

This project is licensed under the MIT License - see the LICENSE file for details.