using Microsoft.EntityFrameworkCore;
using System;

namespace Forecasting
{
    public class ForecastingDbContext : DbContext
    {
        public ForecastingDbContext(DbContextOptions<ForecastingDbContext> options)
            : base(options)
        { 
        }

        public DbSet<NodeMetric>? NodeMetrics { get; set; }
        public DbSet<CpuForecast> CpuForecasts { get; set; }
        public DbSet<MemoryForecast> MemoryForecasts { get; set; }
    }
}
