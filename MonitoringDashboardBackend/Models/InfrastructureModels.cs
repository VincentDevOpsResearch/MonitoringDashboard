using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringDashboard.Models
{
    public class NodeInfo
    {
        public string NodeName { get; set; } = string.Empty;
        public string instanceName { get; set; } = string.Empty;
        public int CpuCores { get; set; }
        public string PhysicalMemory { get; set; } = string.Empty;
        public string DiskCapacity { get; set; } = string.Empty;
        public int CpuUsage { get; set; }
        public int MemoryUsage { get; set; }
        public int DiskUsage { get; set; }
        public string DiskPressure { get; set; } = string.Empty;
        public string MemoryPressure { get; set; } = string.Empty;
        public string PIDPressure { get; set; } = string.Empty;
        public string ReadyStatus { get; set; } = string.Empty;
    }

    public class CpuData
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class CpuForecastData
    {
        public DateTime Timestamp { get; set; }
        public double Mean { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    public class MemoryData
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class MemoryForecastData
    {
        public DateTime Timestamp { get; set; }
        public double Mean { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
    }

    public class ClusterStatus
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int TotalPods { get; set; }
        public int RunningPods { get; set; }
    }

    public class ClusterUtilization
    {
        public int CpuUsage { get; set; }
        public int DiskUsage { get; set; }
        public int RamUsage { get; set; }
        public int CpuStatus { get; set; } // 0 or 1 based on usage threshold
        public int MemoryStatus { get; set; }
        public int DiskStatus { get; set; }
    }

    /// <summary>
    /// Monitors individual node metrics.
    /// </summary>
    public class NodeMetric
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string NodeName { get; set; } = null!; // Node name

        [Required]
        public double CpuUsage { get; set; } // CPU usage percentage (%)

        [Required]
        public double MemoryUsage { get; set; } // Memory usage percentage (%)

        [Required]
        public double DiskUsage { get; set; } // Disk usage percentage (%)

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Data collection timestamp
    }

    /// <summary>
    /// Monitors overall cluster metrics.
    /// </summary>
    public class ClusterMetric
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TotalNodes { get; set; } // Total number of nodes in the cluster

        [Required]
        public int ActiveNodes { get; set; } // Number of active nodes

        [Required]
        public double CpuUsage { get; set; } // Cluster-wide CPU usage percentage (%)

        [Required]
        public double MemoryUsage { get; set; } // Cluster-wide memory usage percentage (%)

        public double? DiskUsage { get; set; } // Cluster-wide disk usage percentage (%), nullable

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Data collection timestamp
    }

    /// <summary>
    /// Monitors pod running status in the cluster.
    /// </summary>
    public class PodMetric
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TotalPods { get; set; } // Total number of pods in the cluster

        [Required]
        public int RunningPods { get; set; } // Number of running pods

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Data collection timestamp
    }

    /// <summary>
    /// Represents CPU usage forecasts.
    /// </summary>
    public class CpuForecast
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("item_id")]
        public string ItemId { get; set; } = null!; // Unique identifier for the forecasted item

        [Required]
        public DateTime Timestamp { get; set; } // Forecasted time

        [Required]
        [Column("mean")]
        public double Mean { get; set; } // Predicted mean memory usage

        [Required]
        [Column("lower_bound")]
        public double LowerBound { get; set; } // Lower bound of the forecasted range

        [Required]
        [Column("upper_bound")]
        public double UpperBound { get; set; } // Upper bound of the forecasted range

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Record creation timestamp
    }

    /// <summary>
    /// Represents Memory usage forecasts.
    /// </summary>
    public class MemoryForecast
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("item_id")]
        public string ItemId { get; set; } = null!; // Unique identifier for the forecasted item

        [Required]
        public DateTime Timestamp { get; set; } // Forecasted time

        [Required]
        [Column("mean")]
        public double Mean { get; set; } // Predicted mean memory usage

        [Required]
        [Column("lower_bound")]
        public double LowerBound { get; set; } // Lower bound of the forecasted range

        [Required]
        [Column("upper_bound")]
        public double UpperBound { get; set; } // Upper bound of the forecasted range

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Record creation timestamp
    }

}
