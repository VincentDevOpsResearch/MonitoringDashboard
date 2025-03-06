
    public class PrometheusQueryResponse
    {
        public string Status { get; set; } = string.Empty;
        public PrometheusData Data { get; set; } = new();
    }

    public class PrometheusData
    {
        public string ResultType { get; set; } = string.Empty;
        public List<PrometheusMetric> Result { get; set; } = new();
    }

    public class PrometheusMetric
    {
        public Dictionary<string, string> Metric { get; set; }
        public object[] Value { get; set; } = new object[2];// Corrected to object[]
        public List<List<object>> Values { get; set; } 
    }