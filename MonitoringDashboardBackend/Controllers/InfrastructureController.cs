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
        public async Task<IActionResult> GetAllNodeCpuForecast()
        {
            var cpuForecastList = await _infrastructureService.GetCpuForecastForAllNodesAsync();
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
