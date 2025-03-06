namespace Polling.Models{
    public class NodeMetric
    {
        public int Id { get; set; }  // Add a primary key
        public string? NodeName { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
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

    public class PodMetric
    {
        public int Id { get; set; }  // Add a primary key
        public int TotalPods { get; set; }
        public int RunningPods { get; set; }
    }

}