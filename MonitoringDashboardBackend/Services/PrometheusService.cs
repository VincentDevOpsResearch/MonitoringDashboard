using MonitoringDashboard.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonitoringDashboard.Services
{
    public class PrometheusService
    {
        private readonly Dictionary<string, HttpClient> _clients;
        private readonly ILogger<PrometheusService> _logger;

        private readonly string _sample_step;

        private readonly string _sample_time;

        public PrometheusService(Dictionary<string, string> prometheusUrls, ILogger<PrometheusService> logger)
        {
            _logger = logger;
            _clients = new Dictionary<string, HttpClient>();

            _sample_time = "1h";
            _sample_step = "5m";

            foreach (var kvp in prometheusUrls)
            {
                _clients[kvp.Key] = new HttpClient { BaseAddress = new Uri(kvp.Value) };
            }
        }

        public async Task<List<PrometheusMetric>> QueryPrometheusAsync(string query, string source = "NodeUrl")
        {
            if (!_clients.ContainsKey(source))
            {
                throw new ArgumentException($"Prometheus source '{source}' not found.");
            }

            var client = _clients[source];

            _logger.LogInformation("Querying Prometheus ({Source}) with query: {Query}", source, query);
            var response = await client.GetAsync($"/api/v1/query?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var prometheusResponse = JsonSerializer.Deserialize<PrometheusQueryResponse>(
                content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


            return prometheusResponse?.Data?.Result ?? new List<PrometheusMetric>();
        }

    }
}
