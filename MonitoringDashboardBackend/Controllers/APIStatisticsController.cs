using Microsoft.AspNetCore.Mvc;
using MonitoringDashboard.Services;
using MonitoringDashboard.Models;
using System.Threading.Tasks;

namespace MonitoringDashboard.Controllers
{
    [Route("/statistics")]
    [ApiController]
    public class ApiStatisticsController : ControllerBase
    {
        private readonly ApiStatisticsService _apiStatisticsService;

        public ApiStatisticsController(ApiStatisticsService apiStatisticsService)
        {
            _apiStatisticsService = apiStatisticsService;
        }

        // Get total requests for a specific time window
        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests([FromQuery] string timeWindow)
        {
            var value = await _apiStatisticsService.GetRequestsAsync(timeWindow);
            return Ok(new ApiResponse<double>(value));
        }

        // Get error rate for a specific time window
        [HttpGet("error-rate")]
        public async Task<IActionResult> GetErrorRate([FromQuery] string timeWindow)
        {
            var value = await _apiStatisticsService.GetErrorRateAsync(timeWindow);
            return Ok(new ApiResponse<double>(value));
        }

        // Get average response time for a specific time window
        [HttpGet("response-time")]
        public async Task<IActionResult> GetResponseTime([FromQuery] string timeWindow)
        {
            var value = await _apiStatisticsService.GetResponseTimeAsync(timeWindow);
            return Ok(new ApiResponse<double>(value));
        }

        // Get request count series
        [HttpGet("requests-series")]
        public async Task<IActionResult> GetRequestsSeries([FromQuery] string timeWindow, [FromQuery] string step)
        {
            var series = await _apiStatisticsService.GetRequestsSeriesAsync(timeWindow, step);
            return Ok(new ApiResponse<MetricSeries>(series));
        }

        // Get error rate series
        [HttpGet("error-rate-series")]
        public async Task<IActionResult> GetErrorRateSeries([FromQuery] string timeWindow, [FromQuery] string step)
        {
            var series = await _apiStatisticsService.GetErrorRateSeriesAsync(timeWindow, step);
            return Ok(new ApiResponse<MetricSeries>(series));
        }

        // Get response time series
        [HttpGet("response-time-series")]
        public async Task<IActionResult> GetResponseTimeSeries([FromQuery] string timeWindow, [FromQuery] string step)
        {
            try {
                var series = await _apiStatisticsService.GetResponseTimeSeriesAsync(timeWindow, step);
                return Ok(new ApiResponse<MetricSeries>(series));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve API performance metrics: {ex.Message}");
            }
        }

        [HttpGet("api-performance")]
        public async Task<ActionResult<List<ApiPerformanceMetrics>>> GetApiPerformanceMetrics()
        {
            try
            {
                var metrics = await _apiStatisticsService.GetApiPerformanceMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve API performance metrics: {ex.Message}");
            }
        }
    }
}
