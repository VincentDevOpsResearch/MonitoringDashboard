using Microsoft.EntityFrameworkCore;
using MonitoringDashboard.Models;

namespace MonitoringDashboard.Data
{
    /// <summary>
    /// Database context for the monitoring dashboard.
    /// </summary>
    public class MonitoringDbContext : DbContext
    {
        public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet for monitoring individual node metrics.
        /// </summary>
        public DbSet<NodeMetric> NodeMetrics { get; set; }

        /// <summary>
        /// DbSet for monitoring overall cluster metrics.
        /// </summary>
        public DbSet<ClusterMetric> ClusterMetrics { get; set; }

        /// <summary>
        /// DbSet for monitoring pod running status.
        /// </summary>
        public DbSet<PodMetric> PodMetrics { get; set; }

        public DbSet<CpuForecast> CpuForecasts { get; set; }
        public DbSet<MemoryForecast> MemoryForecasts { get; set; }
    }
}
