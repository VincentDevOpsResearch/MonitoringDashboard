using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polling.Service;

var builder = Host.CreateDefaultBuilder(args);

// Register services
builder.ConfigureServices((hostContext, services) =>
{
    services.AddLogging();
    services.AddSingleton<PollingService>();  // Ensure PollingService is registered here
});

var app = builder.Build();

// Resolve PollingService and invoke the method
var pollingService = app.Services.GetRequiredService<PollingService>();
await pollingService.QueryAndStoreMetricsAsync();  // Call the polling service method

// The application will complete after the polling task finishes
