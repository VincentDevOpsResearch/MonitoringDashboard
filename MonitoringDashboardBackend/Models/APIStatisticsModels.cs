namespace MonitoringDashboard.Models
{
    // API Response wrapper
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string? ErrorMessage { get; set; }

        public ApiResponse(T data)
        {
            Success = true;
            Data = data;
            ErrorMessage = null;
        }

        public ApiResponse(string errorMessage)
        {
            Success = false;
            Data = default!;
            ErrorMessage = errorMessage;
        }
    }

    // Metric Series for time-based data
    public class MetricSeries
    {
        public List<DateTime> Timestamps { get; set; } = new List<DateTime>();
        public List<double> Values { get; set; } = new List<double>();
    }

    public class ApiPerformanceMetrics
{
    public string ApiEndpoint { get; set; }         
    public string Method { get; set; }             
    public double TotalRequests { get; set; }      
    public double AvgResponseTime { get; set; }    
    public double ErrorRate { get; set; }          
    public double MinResponseTime { get; set; }    
    public double MaxResponseTime { get; set; }   
    public double upperBoundResponseTime { get; set; } 
}
}
