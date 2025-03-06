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

    public class CpuActual
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    public class CpuForecast
    {
        public List<DateTime> Timestamps { get; set; } = new();
        public List<double> ForecastedValues { get; set; } = new();
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
}
