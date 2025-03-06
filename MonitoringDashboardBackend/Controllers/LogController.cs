using Microsoft.AspNetCore.Mvc;
using MonitoringDashboard.Models;
using MonitoringDashboard.Services;

namespace MonitoringDashboard.Controllers
{
    [ApiController]
    [Route("logs")]
    public class LogsController : ControllerBase
    {
        private readonly LogService _logService;

        public LogsController(LogService logService)
        {
            _logService = logService;
        }

        [HttpGet("namespaces")]
        public async Task<ActionResult<List<string>>> GetNamespaces()
        {
            var namespaces = await _logService.GetNamespacesAsync();
            return Ok(namespaces);
        }

        [HttpGet("pods")]
        public async Task<ActionResult<List<string>>> GetPods(string namespaceName)
        {
            var pods = await _logService.GetPodsAsync(namespaceName);
            return Ok(pods);
        }

        [HttpGet("stream")]
        public async Task<IActionResult> GetPaginatedLogs(
            [FromQuery] string namespaceName,
            [FromQuery] string podName,
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            [FromQuery] int maxLines = 100)
        {
            if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(podName))
            {
                return BadRequest("Namespace name and Pod name are required.");
            }

            try
            {
                var response = await _logService.GetPaginatedPodLogsAsync(namespaceName, podName, startTime, endTime, maxLines);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving logs: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving logs.");
            }
        }
    }
}
