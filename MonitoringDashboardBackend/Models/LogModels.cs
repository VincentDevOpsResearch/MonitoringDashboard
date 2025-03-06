namespace MonitoringDashboard.Models
{

    public class PaginatedLogResponse
    {
        public List<LogEntry> Logs { get; set; }
        public DateTime? NextStartTime { get; set; }
        public bool HasMore { get; set; }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Content { get; set; }
    }
}
