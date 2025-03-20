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
                DateTime latestTimestamp = RoundDownToNearestMinutes(DateTime.UtcNow, 5); // Round to nearest 5 minutes
                DateTime timeThreshold = latestTimestamp.AddMinutes(-60); // Use last 60 minutes

                _logger.LogInformation($"latestTimeStamp: {JsonSerializer.Serialize(latestTimestamp)}");

                var pastCpuData = await _dbContext.NodeMetrics
                    .Where(m => m.Timestamp >= timeThreshold && m.Timestamp <= latestTimestamp)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ForecastInput
                    {
                        Timestamp = m.Timestamp,
                        Value = m.CpuUsage,
                        ItemId = m.NodeName // Assuming NodeName is the identifier
                    })
                    .ToListAsync();

                var pastMemoryData = await _dbContext.NodeMetrics
                    .Where(m => m.Timestamp >= timeThreshold && m.Timestamp <= latestTimestamp)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new ForecastInput
                    {
                        Timestamp = m.Timestamp,
                        Value = m.MemoryUsage,
                        ItemId = m.NodeName
                    })
                    .ToListAsync();

                if (!pastCpuData.Any() || !pastMemoryData.Any())
                {
                    _logger.LogWarning("Not enough historical data for forecasting.");
                    return;
                }
                var averagedCpuData = AggregateDataIntoIntervals(pastCpuData, timeThreshold, 5);
                var averagedMemoryData = AggregateDataIntoIntervals(pastMemoryData, timeThreshold, 5);

                _logger.LogInformation($"AggregatedCPU: {JsonSerializer.Serialize(averagedCpuData)}");
                _logger.LogInformation($"AggregatedMemory: {JsonSerializer.Serialize(averagedMemoryData)}");

                var cpuForecasts = await GetCPUForecastAsync("cpu", averagedCpuData);
                var memoryForecasts = await GetMemoryForecastAsync("memory", averagedMemoryData);

                _logger.LogInformation($"cpuForecasts: {JsonSerializer.Serialize(cpuForecasts)}");
                _logger.LogInformation($"memoryForecasts: {JsonSerializer.Serialize(memoryForecasts)}");

                if (cpuForecasts != null && cpuForecasts.Any())
                {
                    _dbContext.CpuForecasts.AddRange(cpuForecasts); 
                }
                else
                {
                    _logger.LogWarning("CPU forecast API returned no results.");
                }

                if (memoryForecasts != null && memoryForecasts.Any())
                {
                    _dbContext.MemoryForecasts.AddRange(memoryForecasts);
                }
                else
                {
                    _logger.LogWarning("Memory forecast API returned no results.");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Forecasting results saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in forecasting: {ex.Message}");
            }
        }

        public List<ForecastInput> AggregateDataIntoIntervals(List<ForecastInput> rawData, DateTime startTime, int intervalMinutes = 5)
        {
            if (!rawData.Any())
                return new List<ForecastInput>();

            List<ForecastInput> aggregatedData = new List<ForecastInput>();

            DateTime endTime = startTime.AddMinutes(60); 

            var groupedData = rawData.GroupBy(d => d.ItemId);

            foreach (var group in groupedData)
            {
                DateTime currentTime = startTime;

                while (currentTime < endTime)
                {
                    var windowData = group.Where(d => d.Timestamp >= currentTime && d.Timestamp < currentTime.AddMinutes(intervalMinutes)).ToList();

                    if (windowData.Any())
                    {
                        aggregatedData.Add(new ForecastInput
                        {
                            Timestamp = currentTime.AddMinutes(intervalMinutes), 
                            Value = (int)Math.Round(windowData.Average(d => d.Value)),
                            ItemId = group.Key
                        });
                    }

                    currentTime = currentTime.AddMinutes(intervalMinutes);
                }
            }

            return aggregatedData.OrderBy(d => d.Timestamp).ToList();
        }
        DateTime RoundDownToNearestMinutes(DateTime dt, int intervalMinutes)
        {
            var totalMinutes = (dt.Hour * 60) + dt.Minute;
            var roundedMinutes = (totalMinutes / intervalMinutes) * intervalMinutes;
            return new DateTime(dt.Year, dt.Month, dt.Day, roundedMinutes / 60, roundedMinutes % 60, 0);
        }


        private async Task<List<CpuForecast>> GetCPUForecastAsync(string type, List<ForecastInput> inputData)
        {
            try
            {
                string apiUrl = $"{_predictionApiUrl}/predict"; 

                List<ForecastInput> updatedInputData = inputData
                    .Select(item => new ForecastInput
                    {
                        Timestamp = item.Timestamp,
                        Value = item.Value,
                        ItemId = item.ItemId + "_cpu"
                    })
                    .ToList();

                var requestContent = new StringContent(JsonSerializer.Serialize(updatedInputData), Encoding.UTF8, "application/json");
                // _logger.LogInformation($"Sending forecast request with data: {JsonSerializer.Serialize(inputData)}");

                var response = await _httpClient.PostAsync(apiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Forecast API error: {response.StatusCode}");
                    return new List<CpuForecast>();
                }
                var forecastResults = await response.Content.ReadFromJsonAsync<List<ForecastResponse>>();

                if (forecastResults == null || !forecastResults.Any())
                {
                    _logger.LogError("Forecast API returned null or empty response.");
                    return new List<CpuForecast>(); 
                }

                var cpuForecasts = forecastResults.Select(forecast => new CpuForecast
                {
                    ItemId = forecast.ItemId,
                    Timestamp = forecast.Timestamp,
                    mean = forecast.mean, 
                    lowerBound = forecast.lowerBound,
                    upperBound = forecast.upperBound
                }).ToList();

                return cpuForecasts;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling forecast API: {ex.Message}");
                return new List<CpuForecast>();
            }
        }


        private async Task<List<MemoryForecast>> GetMemoryForecastAsync(string type, List<ForecastInput> inputData)
        {
            try
            {
                string apiUrl = $"{_predictionApiUrl}/predict"; 

                List<ForecastInput> updatedInputData = inputData
                    .Select(item => new ForecastInput
                    {
                        Timestamp = item.Timestamp,
                        Value = item.Value,
                        ItemId = item.ItemId + "_memory"
                    })
                    .ToList();

                var requestContent = new StringContent(JsonSerializer.Serialize(updatedInputData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(apiUrl, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Forecast API error: {response.StatusCode}");
                    return new List<MemoryForecast>();
                }
                var forecastResults = await response.Content.ReadFromJsonAsync<List<ForecastResponse>>();

                if (forecastResults == null || !forecastResults.Any())
                {
                    _logger.LogError("Forecast API returned null or empty response.");
                    return new List<MemoryForecast>();
                }

                var memoryForecasts = forecastResults.Select(forecast => new MemoryForecast
                {
                    ItemId = forecast.ItemId,
                    Timestamp = forecast.Timestamp,
                    mean = forecast.mean, 
                    lowerBound = forecast.lowerBound,
                    upperBound = forecast.upperBound
                }).ToList();

                return memoryForecasts;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling forecast API: {ex.Message}");
                return new List<MemoryForecast>();
            }
        }
    }
}
