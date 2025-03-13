using Microsoft.AspNetCore.Mvc;
using MonitoringDashboard.Services;
using MonitoringDashboard.Models;
using System.Threading.Tasks;

namespace MonitoringDashboard.Controllers
{
    [Route("infrastructure")]
    [ApiController]
    public class InfrastructureController : ControllerBase
    {
        private readonly InfrastructureService _infrastructureService;

        public InfrastructureController(InfrastructureService infrastructureService)
        {
            _infrastructureService = infrastructureService;
        }

        // Get all Nodes Information
        [HttpGet("nodes/info")]
        public async Task<IActionResult> GetAllNodeInfo()
        {
            var nodeInfoList = await _infrastructureService.GetAllNodeInfoWithConditionsAsync();
            return Ok(nodeInfoList);
        }

        // Get Actual CPU usage for all nodes
        [HttpGet("nodes/cpu-actual")]
        public async Task<IActionResult> GetCpuActualForInstance(string instanceName)
        {
            var cpuActualList = await _infrastructureService.GetCpuActualForInstance(instanceName);
            return Ok(cpuActualList);
        }

        // Get All nodes cpu forecast
        [HttpGet("nodes/cpu-forecast")]
        public async Task<IActionResult> GetCpuForecastForInstance(string instanceName)
        {
            var cpuForecastList = await _infrastructureService.GetCpuForecastForInstance(instanceName);
            return Ok(cpuForecastList);
        }

        [HttpGet("nodes/memory-actual")]
        public async Task<IActionResult> GetMemoryActualForInstance(string instanceName)
        {
            var cpuActualList = await _infrastructureService.GetMemoryActualForInstance(instanceName);
            return Ok(cpuActualList);
        }

        // Get All nodes cpu forecast
        [HttpGet("nodes/memory-forecast")]
        public async Task<IActionResult> GetMemoryActualForecast(string instanceName)
        {
            var cpuForecastList = await _infrastructureService.GetMemoryForecastForInstance(instanceName);
            return Ok(cpuForecastList);
        }

        // Get Cluster Information
        [HttpGet("cluster/status")]
        public async Task<IActionResult> GetClusterStatus()
        {
            var clusterStatus = await _infrastructureService.GetClusterStatusAsync();
            return Ok(clusterStatus);
        }

        // Get Cluster utlization
        [HttpGet("cluster/utilization")]
        public async Task<IActionResult> GetClusterUtilization()
        {
            var utilization = await _infrastructureService.GetResourceUtilizationAsync();
            return Ok(utilization);
        }
    }
}
