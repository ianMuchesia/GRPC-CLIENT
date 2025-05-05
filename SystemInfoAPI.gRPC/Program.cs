


using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using SystemInfoAPI.gRPC.Services;
using SystemInfoAPI.SystemInfoService;

var builder = WebApplication.CreateBuilder(args);


// Add gRPC services to the container
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
    options.MaxSendMessageSize = 5 * 1024 * 1024;    // 5 MB
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS for development
    options.ListenLocalhost(5001, o => o.Protocols = HttpProtocols.Http2);
    
    // For production, use HTTPS endpoint
    options.Listen(IPAddress.Any, 5002, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps(); // In production, you would specify certificate here
    });
});



// Register the SystemInfoProvider as a singleton
builder.Services.AddSingleton<SystemInfoProvider>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.MapGrpcService<SystemInfoGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. " +
                     "To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

Console.WriteLine("Starting System Info gRPC API...");
app.Run();

