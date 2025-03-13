using MonitoringDashboard.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json; 
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace MonitoringDashboard.Services
{
    public class ApiStatisticsService
    {
        private readonly PrometheusService _prometheusService;
        private readonly ILogger<ApiStatisticsService> _logger;
        private readonly string _source;

        public ApiStatisticsService(PrometheusService prometheusService, ILogger<ApiStatisticsService> logger)
        {
            _prometheusService = prometheusService;
            _logger = logger;
            _source = "CustomUrl";
        }

        /// <summary>
        /// Get total requests across all APIs for a specific time window.
        /// </summary>
        public async Task<double> GetRequestsAsync(string timeWindow)
        {
            var query = $"sum(increase(http_requests_total[{timeWindow}]))";
            var result = await _prometheusService.QueryPrometheusAsync(query, _source);
            return result.Any() ? double.Parse(result.First().Value[1].ToString()) : 0;
        }

        /// <summary>
        /// Get error rate across all APIs for a specific time window.
        /// </summary>
        public async Task<double> GetErrorRateAsync(string timeWindow)
        {
            var errorCountQuery = $"sum(increase(http_errors_total[{timeWindow}]))";
            var totalRequestsQuery = $"sum(increase(http_requests_total[{timeWindow}]))";

            var errorCountResult = await _prometheusService.QueryPrometheusAsync(errorCountQuery, _source);
            var totalRequestsResult = await _prometheusService.QueryPrometheusAsync(totalRequestsQuery, _source);

            var errorCount = errorCountResult.Any() ? double.Parse(errorCountResult.First().Value[1].ToString()) : 0;
            var totalRequests = totalRequestsResult.Any() ? double.Parse(totalRequestsResult.First().Value[1].ToString()) : 0;

            return totalRequests > 0 ? (errorCount / totalRequests) * 100 : 0; 
        }

        /// <summary>
        /// Get average response time across all APIs for a specific time window.
        /// </summary>
        public async Task<double> GetResponseTimeAsync(string timeWindow)
        {
            var responseTimeSumQuery = $"sum(increase(http_response_time_sum[{timeWindow}]))";
            var responseTimeCountQuery = $"sum(increase(http_response_time_count[{timeWindow}]))";

            var responseTimeSumResult = await _prometheusService.QueryPrometheusAsync(responseTimeSumQuery, _source);
            var responseTimeCountResult = await _prometheusService.QueryPrometheusAsync(responseTimeCountQuery, _source);

            var responseTimeSum = responseTimeSumResult.Any() ? double.Parse(responseTimeSumResult.First().Value[1].ToString()) : 0;
            var responseTimeCount = responseTimeCountResult.Any() ? double.Parse(responseTimeCountResult.First().Value[1].ToString()) : 0;

            return responseTimeCount > 0 ? responseTimeSum / responseTimeCount : 0; 
        }
        /// <summary>
        /// Get time series of request counts.
        /// </summary>
        public async Task<MetricSeries> GetRequestsSeriesAsync(string timeWindow, string step)
        {
            var query = $"sum(increase(http_requests_total[{step}]))[{timeWindow}:{step}]";
            var result = await _prometheusService.QueryPrometheusAsync(query, _source);
            return ParseMetricSeries(result, timeWindow, step);
        }

        /// <summary>
        /// Get time series of error rates.
        /// </summary>
        public async Task<MetricSeries> GetErrorRateSeriesAsync(string timeWindow, string step)
        {
            var errorCountQuery = $"sum(increase(http_errors_total[{step}]))[{timeWindow}:{step}]";
            var totalRequestsQuery = $"sum(increase(http_requests_total[{step}]))[{timeWindow}:{step}]";

            var errorCountResult = await _prometheusService.QueryPrometheusAsync(errorCountQuery, _source);
            var totalRequestsResult = await _prometheusService.QueryPrometheusAsync(totalRequestsQuery, _source);

            var errorCountSeries = ParseMetricSeries(errorCountResult, timeWindow, step);
            var totalRequestsSeries = ParseMetricSeries(totalRequestsResult, timeWindow, step);

            var errorRateSeries = new MetricSeries
            {
                Timestamps =  totalRequestsSeries.Timestamps,
                Values = new List<double>()
            };

            for (int i = 0; i < totalRequestsSeries.Values.Count; i++)
            {
                double errorCount = i < errorCountSeries.Values.Count ? errorCountSeries.Values[i] : 0; // Handle missing errorCount data
                double totalRequests = totalRequestsSeries.Values[i];
                double errorRate = totalRequests > 0 ? (errorCount / totalRequests) * 100 : 0; 
                errorRateSeries.Values.Add(Math.Round(errorRate, 2)); 
            }

            return errorRateSeries;
        }

        /// <summary>
        /// Get time series of average response times.
        /// </summary>
        public async Task<MetricSeries> GetResponseTimeSeriesAsync(string timeWindow, string step)
        {
            var responseTimeSumQuery = $"sum(increase(http_response_time_count[{step}]))[{timeWindow}:{step}]";
            var responseTimeCountQuery = $"sum(increase(http_requests_total[{step}]))[{timeWindow}:{step}]";

            var responseTimeSumResult = await _prometheusService.QueryPrometheusAsync(responseTimeSumQuery, _source);
            var responseTimeCountResult = await _prometheusService.QueryPrometheusAsync(responseTimeCountQuery, _source);

            var responseTimeSumSeries = ParseMetricSeries(responseTimeSumResult, timeWindow, step);
            var responseTimeCountSeries = ParseMetricSeries(responseTimeCountResult, timeWindow, step);

            var avgResponseTimeSeries = new MetricSeries
            {
                Timestamps = responseTimeSumSeries.Timestamps,
                Values = new List<double>()
            };

            for (int i = 0; i < responseTimeSumSeries.Values.Count; i++)
            {
                double responseTimeSum = responseTimeSumSeries.Values[i];
                double responseTimeCount = responseTimeCountSeries.Values[i];
                double avgResponseTime = responseTimeCount > 0 ? responseTimeSum / responseTimeCount : 0; // 防止分母为零
                avgResponseTimeSeries.Values.Add(Math.Round(avgResponseTime, 2)); 
            }

            return avgResponseTimeSeries;
        }

        // Helper method: Parse PromQL time strings into TimeSpan
        private TimeSpan ParsePromQLTime(string time)
        {
            if (string.IsNullOrEmpty(time))
                throw new ArgumentException("Time string cannot be null or empty.");

            var unit = time[^1];
            var value = double.Parse(time[..^1]);

            return unit switch
            {
                's' => TimeSpan.FromSeconds(value),
                'm' => TimeSpan.FromMinutes(value),
                'h' => TimeSpan.FromHours(value),
                'd' => TimeSpan.FromDays(value),
                _ => throw new ArgumentException($"Invalid PromQL time unit: {unit}"),
            };
        }

        private MetricSeries ParseMetricSeries(List<PrometheusMetric> result, string timeWindow, string step)
        {
            var series = new MetricSeries
            {
                Timestamps = new List<DateTime>(),
                Values = new List<double>()
            };

            if (result == null || result.Count == 0)
            {
                _logger.LogWarning("No metrics available to parse.");
                return series;
            }

            var stepDuration = ParsePromQLTime(step);
            var timeWindowDuration = ParsePromQLTime(timeWindow);

            // Align currentTime to the start of the current hour
            var currentTime = DateTime.UtcNow;
            currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0, DateTimeKind.Utc);

            // Calculate the start of the series
            var seriesStartTime = currentTime - timeWindowDuration;

            // Process the first metric entry
            var metric = result.First();

            if (metric.Values == null || metric.Values.Count == 0)
            {
                _logger.LogWarning("Metric has no values to process.");
                return series;
            }

            // Map Prometheus timestamps (as DateTime) to values
            var dataPoints = metric.Values.ToDictionary(
                v =>
                {
                    var timestamp = v[0];
                    if (timestamp is JsonElement element && element.TryGetInt64(out var unixTime))
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                    }

                    throw new InvalidCastException("Failed to parse timestamp from Prometheus data.");
                },
                v =>
                {
                    var value = v[1];
                    if (value is JsonElement element && element.ValueKind == JsonValueKind.String &&
                        double.TryParse(element.GetString(), out var parsedValue))
                    {
                        return Math.Floor(parsedValue); // Ensure integer values by flooring
                    }

                    return 0; // Default to 0 if parsing fails
                }
            );

            // Fill the series with timestamps and corresponding values (or 0 if missing)
            for (var time = seriesStartTime; time <= currentTime; time += stepDuration)
            {
                series.Timestamps.Add(time);
                series.Values.Add(dataPoints.TryGetValue(time, out var value) ? value : 0);
            }

            return series;
        }

         /// <summary>
        /// performance for eacht endpoints
        /// </summary>
        public async Task<List<ApiPerformanceMetrics>> GetApiPerformanceMetricsAsync()
        {
            var totalRequestsQuery = "sum by (endpoint, method) (http_requests_total)";
            var totalRequestsResult = await _prometheusService.QueryPrometheusAsync(totalRequestsQuery, _source);

            var errorCountQuery = "sum by (endpoint, method) (http_errors_total)";
            var errorCountResult = await _prometheusService.QueryPrometheusAsync(errorCountQuery, _source);

            var responseTimeSumQuery = "sum by (endpoint, method) (http_response_time_sum)";
            var responseTimeSumResult = await _prometheusService.QueryPrometheusAsync(responseTimeSumQuery, _source);

            var responseTimeCountQuery = "sum by (endpoint, method) (http_response_time_count)";
            var responseTimeCountResult = await _prometheusService.QueryPrometheusAsync(responseTimeCountQuery, _source);

            var responseTimeBucketQuery = "sum by (endpoint, method, le) (http_response_time_bucket)";
            var responseTimeBucketResult = await _prometheusService.QueryPrometheusAsync(responseTimeBucketQuery, _source);

            var performanceMetrics = new List<ApiPerformanceMetrics>();

            foreach (var metric in totalRequestsResult)
            {
                var labels = metric.Metric;
                var endpoint = labels["endpoint"];
                var method = labels["method"];

                var totalRequests = double.Parse(metric.Value[1].ToString());

                var errorCountMetric = errorCountResult.FirstOrDefault(m =>
                    m.Metric["endpoint"] == endpoint && m.Metric["method"] == method);
                var errorCount = errorCountMetric != null ? double.Parse(errorCountMetric.Value[1].ToString()) : 0;

                var responseTimeSumMetric = responseTimeSumResult.FirstOrDefault(m =>
                    m.Metric["endpoint"] == endpoint && m.Metric["method"] == method);
                var responseTimeSum = responseTimeSumMetric != null ? double.Parse(responseTimeSumMetric.Value[1].ToString()) : 0;

                var responseTimeCountMetric = responseTimeCountResult.FirstOrDefault(m =>
                    m.Metric["endpoint"] == endpoint && m.Metric["method"] == method);
                var responseTimeCount = responseTimeCountMetric != null ? double.Parse(responseTimeCountMetric.Value[1].ToString()) : 0;

                var avgResponseTime = responseTimeCount > 0 ? responseTimeSum / responseTimeCount : 0;
                var errorRate = totalRequests > 0 ? (errorCount / totalRequests) * 100 : 0;

                // Calculate min, max, and 90th percentile from buckets
                var buckets = responseTimeBucketResult
                    .Where(m => m.Metric["endpoint"] == endpoint && m.Metric["method"] == method)
                    .Select(m => new
                    {
                        Le = m.Metric["le"],
                        Value = double.Parse(m.Value[1].ToString())
                    })
                    .OrderBy(b => b.Le == "+Inf" ? double.MaxValue : double.Parse(b.Le)) // Order by le
                    .ToList();

                double minResponseTime = buckets.FirstOrDefault()?.Le == null ? 0 : double.Parse(buckets.FirstOrDefault()?.Le ?? "0");
                double maxResponseTime = buckets.LastOrDefault()?.Le == "+Inf" ? buckets[^2].Value : double.Parse(buckets.LastOrDefault()?.Le ?? "0");

                double totalBucketCount = buckets.LastOrDefault()?.Value ?? 0;
                double threshold = totalBucketCount * 0.9;

                double cumulativeCount = 0;
                double upperBoundResponseTime = 0;

                foreach (var bucket in buckets)
                {
                    cumulativeCount += bucket.Value;
                    if (cumulativeCount >= threshold)
                    {
                        upperBoundResponseTime = bucket.Le == "+Inf" ? maxResponseTime : double.Parse(bucket.Le);
                        break;
                    }
                }

                performanceMetrics.Add(new ApiPerformanceMetrics
                {
                    ApiEndpoint = endpoint,
                    Method = method,
                    TotalRequests = totalRequests,
                    AvgResponseTime = avgResponseTime,
                    MinResponseTime = minResponseTime,
                    MaxResponseTime = maxResponseTime,
                    upperBoundResponseTime = upperBoundResponseTime,
                    ErrorRate = errorRate
                });
            }

            return performanceMetrics;
        }
    }
}
