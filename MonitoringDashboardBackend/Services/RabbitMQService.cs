using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringDashboard.Models;
using System.Linq;

namespace MonitoringDashboard.Services
{
    public class RabbitMQService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(HttpClient httpClient, IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            _httpClient = httpClient;

            var rabbitMQConfig = configuration.GetSection("RabbitMQ");
            _baseUrl = rabbitMQConfig.GetValue<string>("BaseUrl");
            var username = rabbitMQConfig.GetValue<string>("Username");
            var password = rabbitMQConfig.GetValue<string>("Password");

            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new ArgumentException("RabbitMQ BaseUrl is not configured.");

            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }

        public async Task<object> GetRabbitMQOverviewWithGraphAsync(int lengths_age, int lengths_incr, int msg_rages_age, int msg_rates_incr)
        {
            var url = $"/api/overview?lengths_age={lengths_age}&lengths_incr={lengths_incr}&msg_rates_age={msg_rages_age}&msg_rates_incr={msg_rates_incr}";

            try
            {
                _logger.LogInformation("Fetching RabbitMQ overview data from {Url}", url);

                var response = await _httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                var queuedMessages = root.GetProperty("queue_totals").GetProperty("messages").GetInt32();
                var unacknowledged = root.GetProperty("queue_totals").GetProperty("messages_unacknowledged").GetInt32();
                var queues = root.GetProperty("object_totals").GetProperty("queues").GetInt32();
                var consumers = root.GetProperty("object_totals").GetProperty("consumers").GetInt32();
                var channels = root.GetProperty("object_totals").GetProperty("channels").GetInt32();

                var incomingRateSamples = root
                    .GetProperty("message_stats")
                    .GetProperty("disk_reads_details")
                    .GetProperty("samples");

                var incomingRate = incomingRateSamples.GetArrayLength() > 0
                    ? incomingRateSamples.EnumerateArray().Last().GetProperty("sample").GetInt32()
                    : 0;

                var messageRateSamples = root
                    .GetProperty("message_stats")
                    .GetProperty("disk_writes_details")
                    .GetProperty("samples");

                var messageRateGraph = messageRateSamples
                    .EnumerateArray()
                    .Select(sample => new RabbitMQMessageRateData
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(sample.GetProperty("timestamp").GetInt64()).DateTime,
                        Sample = sample.GetProperty("sample").GetInt32()
                    })
                    .Reverse().ToList();

                var queuedMessagesSamples = root
                    .GetProperty("queue_totals")
                    .GetProperty("messages_details")
                    .GetProperty("samples");

                var queuedMessagesGraph = queuedMessagesSamples
                    .EnumerateArray()
                    .Select(sample => new
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(sample.GetProperty("timestamp").GetInt64()).UtcDateTime,
                        Sample = sample.GetProperty("sample").GetInt32()
                    })
                    .Reverse().ToList();

                _logger.LogInformation("Successfully fetched RabbitMQ overview data.");

                return new
                {
                    Queues = queues,
                    Consumers = consumers,
                    Channels = channels,
                    IncomingRate = incomingRate,
                    Unacknowledged = unacknowledged,
                    QueuedMessages = queuedMessages,
                    MessageRateGraph = messageRateGraph,
                    QueuedMessageGraph = queuedMessagesGraph
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch RabbitMQ overview data from {Url}", url);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching RabbitMQ overview data.");
                throw;
            }
        }

        public async Task<List<QueueInfo>> GetQueuesAsync(int page = 1, int pageSize = 100)
        {
            var url = $"{_baseUrl}/api/queues?page={page}&page_size={pageSize}&use_regex=false&pagination=true";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to retrieve RabbitMQ queues data.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            var queues = jsonDoc
                .RootElement
                .GetProperty("items")
                .EnumerateArray()
                .Select(queue => new QueueInfo
                {
                    VirtualHost = queue.GetProperty("vhost").GetString(),
                    Name = queue.GetProperty("name").GetString(),
                    Type = queue.GetProperty("type").GetString(),
                    State = queue.GetProperty("state").GetString(),
                    ReadyMessages = queue.GetProperty("messages_ready").GetInt32(),
                    UnackedMessages = queue.GetProperty("messages_unacknowledged").GetInt32(),
                    TotalMessages = queue.GetProperty("messages").GetInt32(),
                    IncomingRate = queue.GetProperty("messages_ready_details").GetProperty("rate").GetDouble(),
                    UnackedRate = queue.GetProperty("messages_unacknowledged_details").GetProperty("rate").GetDouble()
                })
                .ToList();

            return queues;
        }
    }
}
