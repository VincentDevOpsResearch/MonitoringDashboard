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

        // Query Node CPU Usage
        public async Task<double> GetCpuUsageAsync(string instanceName)
        {
            var query = $@"
            (
                sum(rate(node_cpu_seconds_total{{mode!=""idle"", instance=""{instanceName}""}}[{_sample_step}]))
                /
                sum(rate(node_cpu_seconds_total{{instance=""{instanceName}""}}[{_sample_step}]))
            )";
            var prometheusResponse = await QueryPrometheusAsync(query);

            var result = prometheusResponse.FirstOrDefault()?.Value?[1]?.ToString();

            return double.TryParse(result, out var utilization) ? Math.Round(utilization * 100, 2) : 0;
        }



        // Query Node Memory Usage
        public async Task<double> GetMemoryUsageAsync(string instanceName)
        {
            var memoryUsageData = await QueryPrometheusAsync($"(1 - (node_memory_MemAvailable_bytes{{instance=\"{instanceName}\"}} / node_memory_MemTotal_bytes{{instance=\"{instanceName}\"}})) * 100");
            return memoryUsageData.Any() ? Math.Round(double.Parse(memoryUsageData.First().Value[1].ToString()), 2) : 0;
        }

        // Query Node Disk Usage
        public async Task<double> GetDiskUsageAsync(string instanceName)
        {
            var diskUsageData = await QueryPrometheusAsync($"(1 - (node_filesystem_avail_bytes{{instance=\"{instanceName}\", fstype!=\"tmpfs\"}} / node_filesystem_size_bytes{{instance=\"{instanceName}\", fstype!=\"tmpfs\"}})) * 100");
            return diskUsageData.Any() ? Math.Round(double.Parse(diskUsageData.First().Value[1].ToString()), 2) : 0;
        }


        public async Task<List<CpuActual>> GetCpuActualForInstance(string instanceName)
        {
           var query = $@"
            (
                sum(rate(node_cpu_seconds_total{{mode!=""idle"", instance=""{instanceName}""}}[{_sample_step}]))
                /
                sum(rate(node_cpu_seconds_total{{instance=""{instanceName}""}}[{_sample_step}]))
            )[{_sample_time}:{_sample_step}]";

            try
            {
                var result = await QueryPrometheusAsync(query);

                if (result == null || result.Count == 0)
                {
                    _logger.LogWarning("No CPU data found for instance {InstanceName}.", instanceName);
                    return new List<CpuActual>();
                }

                var utcOffset = TimeSpan.FromHours(8);

                var cpuActualData = result.FirstOrDefault()?.Values
                    ?.Select(entry =>
                    {
                        var timestamp = DateTimeOffset.FromUnixTimeSeconds((long)((JsonElement)entry[0]).GetDouble())
                                        .ToOffset(utcOffset)
                                        .DateTime;
                        var value = double.TryParse(((JsonElement)entry[1]).GetString(), out var parsedValue)
                            ? Math.Round(parsedValue * 100, 2)
                            : 0;

                        return new CpuActual
                        {
                            Timestamp = timestamp,
                            Value = value
                        };
                    })
                    ?.ToList();

                if (cpuActualData != null && cpuActualData.Count > 0)
                {
                    var filledData = new List<CpuActual>();
                    var startTime = cpuActualData.First().Timestamp;
                    var endTime = cpuActualData.Last().Timestamp;

                    for (var time = startTime; time <= endTime; time = time.AddMinutes(5))
                    {
                        var existingData = cpuActualData.FirstOrDefault(d => d.Timestamp == time);
                        if (existingData != null)
                        {
                            filledData.Add(existingData);
                        }
                        else
                        {
                            filledData.Add(new CpuActual
                            {
                                Timestamp = time,
                                Value = 0 
                            });
                        }
                    }

                    cpuActualData = filledData;
                }

                _logger.LogInformation("Retrieved CPU actual usage data for instance {InstanceName} with UTC+8 adjustment.", instanceName);
                return cpuActualData ?? new List<CpuActual>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve CPU actual data for instance {InstanceName}.", instanceName);
                throw;
            }
        }



        public async Task<List<CpuForecast>> GetCpuForecastForAllNodesAsync()
        {
            _logger.LogInformation("Fetching CPU forecast data for all nodes from Prometheus.");
            var forecastData = await QueryPrometheusAsync("forecast_query_here");

            var cpuForecastList = forecastData.Select(f => new CpuForecast
            {
                Timestamps = new List<DateTime>(),
                ForecastedValues = new List<double>()
            }).ToList();

            _logger.LogInformation("Retrieved CPU forecast data for {Count} nodes.", cpuForecastList.Count);
            return cpuForecastList;
        }

        public async Task<ClusterStatus> GetClusterStatusAsync()
        {
            _logger.LogInformation("Fetching cluster status from Prometheus.");

            var totalNodesData = await QueryPrometheusAsync("count(kube_node_info)");
            var activeNodesData = await QueryPrometheusAsync("count(kube_node_status_condition{condition=\"Ready\",status=\"true\"})");

            // only query pods in default namespace
            var totalPodsData = await QueryPrometheusAsync("count(kube_pod_info{namespace=\"default\"})");
            var runningPodsData = await QueryPrometheusAsync("count(kube_pod_status_phase{phase=\"Running\", namespace=\"default\"})");

            var clusterStatus = new ClusterStatus
            {
                TotalNodes = int.Parse(totalNodesData.FirstOrDefault()?.Value[1].ToString() ?? "0"),
                ActiveNodes = int.Parse(activeNodesData.FirstOrDefault()?.Value[1].ToString() ?? "0"),
                TotalPods = int.Parse(totalPodsData.FirstOrDefault()?.Value[1].ToString() ?? "0"),
                RunningPods = int.Parse(runningPodsData.FirstOrDefault()?.Value[1].ToString() ?? "0")
            };


            return clusterStatus;
        }


        public async Task<ClusterUtilization> GetClusterUtilizationAsync()
        {
            _logger.LogInformation("Fetching cluster utilization data from Prometheus.");

            var query = $@"
            (
                sum(rate(node_cpu_seconds_total{{mode!=""idle""}}[{_sample_step}]))
                /
                sum(rate(node_cpu_seconds_total{{}}[{_sample_step}]))
            )";

            var cpuUtilizationData = await QueryPrometheusAsync(query);
            var cpuUsage = cpuUtilizationData.FirstOrDefault()?.Value?[1]?.ToString();
            _logger.LogInformation($"Cluster utilization retrieved: CPU Usage = {cpuUsage}");

            var diskUtilizationData = await QueryPrometheusAsync("(1 - (node_filesystem_avail_bytes{fstype!=\"tmpfs\"} / node_filesystem_size_bytes{fstype!=\"tmpfs\"})) * 100");
            var diskUsage = diskUtilizationData.FirstOrDefault()?.Value[1]?.ToString();

            var memoryUtilizationData = await QueryPrometheusAsync("(1 - (sum(node_memory_MemAvailable_bytes) by (instance) / sum(node_memory_MemTotal_bytes) by (instance))) * 100");
            var memoryUsage = memoryUtilizationData.FirstOrDefault()?.Value[1]?.ToString();

            var clusterUtilization = new ClusterUtilization
            {
                CpuUsage = cpuUsage != null ? (int)Math.Round(double.Parse(cpuUsage) * 100) : 0,
                DiskUsage = diskUsage != null ? (int)Math.Round(double.Parse(diskUsage)) : 0,
                RamUsage = memoryUsage != null ? (int)Math.Round(double.Parse(memoryUsage)) : 0
            };

            _logger.LogInformation("Cluster utilization retrieved: CPU Usage = {CpuUsage}%, Disk Usage = {DiskUsage}%, RAM Usage = {RamUsage}%", 
                                clusterUtilization.CpuUsage, clusterUtilization.DiskUsage, clusterUtilization.RamUsage);

            return clusterUtilization;
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
