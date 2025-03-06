using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forecasting
{
    // Model for Node Metrics
    public class NodeMetric
    {
        public int Id { get; set; }  // Add a primary key
        public string? NodeName { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    
    // Model for CPU Forecast
    public class CpuForecast
    {
        public int Id { get; set; }
        
        
        [Column("item_id")]
        public string ItemId { get; set; }
        public DateTime Timestamp { get; set; }
        
        [Column("mean")]
        public float Mean { get; set; }
        [Column("percentile_10")]
        public float Percentile10 { get; set; }
        
        [Column("percentile_90")]
        public float Percentile90 { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    // Model for Memory Forecast
    public class MemoryForecast
    {
        public int Id { get; set; }
        
        
        [Column("item_id")]
        public string ItemId { get; set; }
        public DateTime Timestamp { get; set; }
        
        [Column("mean")]
        public float Mean { get; set; }
        [Column("percentile_10")]
        public float Percentile10 { get; set; }
        
        [Column("percentile_90")]
        public float Percentile90 { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class ClusterMetric
    {
        public int Id { get; set; }  // Add a primary key
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
    }



    // Model for Forecast Input Data
    public class ForecastInput
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string ItemId { get; set; }
    }

    // Model for Forecast API Response
    public class ForecastResponse
    {
        public DateTime Timestamp { get; set; }
        public float Mean { get; set; }
        public float Percentile10 { get; set; }
        public float Percentile90 { get; set; }
    }
}
