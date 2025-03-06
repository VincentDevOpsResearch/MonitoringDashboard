using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json; 
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Forecasting
{
    public class ForecastingService
    {
        private readonly ILogger<ForecastingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ForecastingDbContext _dbContext;
        private readonly string _predictionApiUrl;

        public ForecastingService(ILogger<ForecastingService> logger, HttpClient httpClient, ForecastingDbContext dbContext)
        {
            _logger = logger;
            _httpClient = httpClient;
            _dbContext = dbContext;
            _predictionApiUrl = Environment.GetEnvironmentVariable("PREDICTION_API_URL")
                ?? throw new InvalidOperationException("Prediction API URL is not set.");
        }

        public async Task ExecuteForecastingAsync()
        {
            _logger.LogInformation("Fetching historical metrics for forecasting...");

            try
            {
                var timeThreshold = DateTime.UtcNow.AddMinutes(-60); // Retrieve last 60 minutes of data

                var pastCpuData = await _dbContext.NodeMetrics
                    .Where(m => m.Timestamp >= timeThreshold)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ForecastInput
                    {
                        Timestamp = m.Timestamp,
                        Value = m.CpuUsage,  // Use actual CPU usage data
                        ItemId = m.NodeName // Assuming NodeName is the identifier
                    })
                    .ToListAsync();

                var pastMemoryData = await _dbContext.NodeMetrics
                    .Where(m => m.Timestamp >= timeThreshold)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ForecastInput
                    {
                        Timestamp = m.Timestamp,
                        Value = m.MemoryUsage,  // Use actual memory usage data
                        ItemId = m.NodeName // Assuming NodeName is the identifier
                    })
                    .ToListAsync();


                if (!pastCpuData.Any() || !pastMemoryData.Any())
                {
                    _logger.LogWarning("Not enough historical data for forecasting.");
                    return;
                }

                // Aggregate data into 5-minute intervals
                var averagedCpuData = AggregateDataIntoIntervals(pastCpuData, 5);
                var averagedMemoryData = AggregateDataIntoIntervals(pastMemoryData, 5);

                // Call the prediction API
                var cpuForecast = await GetCPUForecastAsync("cpu", averagedCpuData);
                var memoryForecast = await GetMemoryForecastAsync("memory", averagedMemoryData);

                if (cpuForecast != null)
                {
                    _dbContext.CpuForecasts.Add(cpuForecast);
                    _logger.LogInformation($"Stored CPU forecast for {cpuForecast.Timestamp}.");
                }

                if (memoryForecast != null)
                {
                    _dbContext.MemoryForecasts.Add(memoryForecast);
                    _logger.LogInformation($"Stored Memory forecast for {memoryForecast.Timestamp}.");
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in forecasting: {ex.Message}");
            }
        }

        private List<ForecastInput> AggregateDataIntoIntervals(List<ForecastInput> rawData, int intervalMinutes)
        {
            if (!rawData.Any()) return new List<ForecastInput>();

            List<ForecastInput> aggregatedData = new List<ForecastInput>();

            DateTime startTime = rawData.Min(d => d.Timestamp);
            DateTime endTime = rawData.Max(d => d.Timestamp);
            DateTime currentTime = startTime;

            var groupedData = rawData.GroupBy( d => d.ItemId);

            foreach (var group in groupedData)
            {
                while (currentTime <= endTime)
                {
                    var windowData = rawData
                        .Where(d => d.Timestamp >= currentTime && d.Timestamp < currentTime.AddMinutes(intervalMinutes))
                        .ToList();

                    if (windowData.Any())
                    {
                        aggregatedData.Add(new ForecastInput
                        {
                            Timestamp = currentTime,
                            Value = windowData.Average(d => d.Value),
                            // ItemId = windowData.First().ItemId
                            ItemId = "Node1"
                        });
                    }

                    currentTime = currentTime.AddMinutes(intervalMinutes);
                }
            }

            return aggregatedData;
        }

        private async Task<CpuForecast> GetCPUForecastAsync(string type, List<ForecastInput> inputData)
        {
            try
            {
                string apiUrl = $"{_predictionApiUrl}/predict"; 

                var requestContent = new StringContent(JsonSerializer.Serialize(inputData), Encoding.UTF8, "application/json");
                _logger.LogInformation($"Sending forecast request with data: {JsonSerializer.Serialize(inputData)}");
                var response = await _httpClient.PostAsync(apiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Forecast API error: {response.StatusCode}");
                    return null;
                }

                var forecastResult = await response.Content.ReadFromJsonAsync<ForecastResponse>();

                if (forecastResult == null)
                {
                    _logger.LogError("Forecast API returned null response.");
                    return null;
                }

                return new CpuForecast
                {
                    ItemId = inputData.First().ItemId,
                    Timestamp = forecastResult.Timestamp,
                    Mean = forecastResult.Mean,
                    Percentile10 = forecastResult.Percentile10,
                    Percentile90 = forecastResult.Percentile90
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling forecast API: {ex.Message}");
                return null;
            }
        }

        private async Task<MemoryForecast> GetMemoryForecastAsync(string type, List<ForecastInput> inputData)
        {
            try
            {
                string apiUrl = $"{_predictionApiUrl}/predict"; 

                var requestContent = new StringContent(JsonSerializer.Serialize(inputData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Forecast API error: {response.StatusCode}");
                    return null;
                }

                var forecastResult = await response.Content.ReadFromJsonAsync<ForecastResponse>();

                if (forecastResult == null)
                {
                    _logger.LogError("Forecast API returned null response.");
                    return null;
                }

                return new MemoryForecast
                {
                    ItemId = inputData.First().ItemId,
                    Timestamp = forecastResult.Timestamp,
                    Mean = forecastResult.Mean,
                    Percentile10 = forecastResult.Percentile10,
                    Percentile90 = forecastResult.Percentile90
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling forecast API: {ex.Message}");
                return null;
            }
        }
    }
}
