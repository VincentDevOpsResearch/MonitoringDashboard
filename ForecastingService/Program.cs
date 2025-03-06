using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Forecasting;
using System;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // Register logging
    services.AddLogging();
    
    // Register HttpClient
    services.AddHttpClient();

    // Register ForecastingDbContext with the connection string
    var connectionString = Environment.GetEnvironmentVariable("MONITORING_DB_CONNECTION_STRING")
                           ?? throw new InvalidOperationException("Database connection string is not set.");
    services.AddDbContext<ForecastingDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Register ForecastingService
    services.AddSingleton<ForecastingService>();
});

var app = builder.Build();

// Resolve ForecastingService from DI container
var forecastingService = app.Services.GetRequiredService<ForecastingService>();

// Execute the forecasting logic
await forecastingService.ExecuteForecastingAsync();
