namespace MonitoringDashboard.Models
{
    public class RabbitMQMessageRateData
    {
        public DateTime Timestamp { get; set; }
        public int Sample { get; set; }
    }

    public class RabbitMQOverviewWithGraph
    {
        public int Queues { get; set; }
        public int Consumers { get; set; }
        public int Channels { get; set; }
        public int IncomingRate { get; set; }
        public int Unacknowledged { get; set; }
        public int QueuedMessages { get; set; }
        public List<RabbitMQMessageRateData> MessageRateGraph { get; set; }

        public List<RabbitMQMessageRateData> QueuedMessageGraph { get; set; }
    }
    
    public class QueueInfo
    {
        public string VirtualHost { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public int ReadyMessages { get; set; }
        public int UnackedMessages { get; set; }
        public int TotalMessages { get; set; }
        public double IncomingRate { get; set; }
        public double UnackedRate { get; set; }
    }
}
