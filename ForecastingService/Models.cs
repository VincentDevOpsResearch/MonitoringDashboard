using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        public float mean { get; set; }
        [Column("lower_bound")]
        public float lowerBound { get; set; }
        
        [Column("upper_bound")]
        public float upperBound { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Model for Memory Forecast
    public class MemoryForecast
    {
        public int Id { get; set; }
        
        
        [Column("item_id")]
        public string ItemId { get; set; }
        public DateTime Timestamp { get; set; }
        
        [Column("mean")]
        public float mean { get; set; }
        [Column("lower_bound")]
        public float lowerBound { get; set; }
        
        [Column("upper_bound")]
        public float upperBound { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; }
    }

    // Model for Forecast API Response
    public class ForecastResponse
    {
        [JsonPropertyName("item_id")]
        public string ItemId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("prediction")]
        public float mean { get; set; }

        public float lowerBound { get; set; }

        public float upperBound { get; set; }
    }
}
