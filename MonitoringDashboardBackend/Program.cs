using MonitoringDashboard.Services;
using MonitoringDashboard.Data;
using Microsoft.EntityFrameworkCore;
using k8s;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("MONITORING_DB_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is not set. Please define MONITORING_DB_CONNECTION_STRING.");
}

// Add database context with SQL Server
builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlServer(connectionString));


// Add Kubernetes Client
builder.Services.AddSingleton<IKubernetes>(_ =>
{
    var config = KubernetesClientConfiguration.BuildDefaultConfig();
    return new Kubernetes(config);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Register Services
builder.Services.AddSingleton<PrometheusService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<PrometheusService>>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var prometheusUrls = configuration.GetSection("Prometheus").Get<Dictionary<string, string>>();
    return new PrometheusService(prometheusUrls, logger);
});

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("MONITORING_DB_CONNECTION_STRING")));

builder.Services.AddSingleton<ApiStatisticsService>();
builder.Services.AddHttpClient<RabbitMQService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<InfrastructureService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
