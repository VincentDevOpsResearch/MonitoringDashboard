namespace MonitoringDashboard.Models{
    public class AlertThreshold
    {
        public string Category { get; set; } = string.Empty;  
        public double Threshold { get; set; }  
        public int Mode { get; set; }  // 0 = < , 1 = must equal
    }

    public class AlertUpdateRequest
    {
        public string Category { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public int Mode { get; set; }
    }
}