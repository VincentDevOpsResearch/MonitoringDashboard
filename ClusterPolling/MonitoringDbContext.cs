#nullable enable

using Microsoft.EntityFrameworkCore;
using Polling.Models;

public class MonitoringDbContext : DbContext
{
    private readonly string _connectionString;

    public MonitoringDbContext()
    {
        _connectionString = Environment.GetEnvironmentVariable("MONITORING_DB_CONNECTION_STRING")
                            ?? throw new InvalidOperationException("Database connection string is not set.");
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);
    }

    public DbSet<NodeMetric>? NodeMetrics { get; set; }
    public DbSet<ClusterMetric>? ClusterMetrics { get; set; }
    public DbSet<PodMetric>? PodMetrics { get; set; }
}
