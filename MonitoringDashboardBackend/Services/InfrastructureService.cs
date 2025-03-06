using k8s;
using System.Text.Json;
using MonitoringDashboard.Models;

namespace MonitoringDashboard.Services
{
    public class InfrastructureService
    {
        private readonly PrometheusService _prometheusService;
        private readonly Kubernetes _client;
        private readonly ILogger<InfrastructureService> _logger;

        public InfrastructureService(PrometheusService prometheusService, ILogger<InfrastructureService> logger)
        {
            _prometheusService = prometheusService;
            _logger = logger;

            var config =  KubernetesClientConfiguration.BuildDefaultConfig();
            _client = new Kubernetes(config);
        }

        // Get all node info
        public async Task<List<NodeInfo>> GetAllNodeInfoWithConditionsAsync()
        {
            _logger.LogInformation("Fetching all nodes from Kubernetes...");

            var nodes = await _client.CoreV1.ListNodeAsync();
            var nodesJson = JsonSerializer.Serialize(nodes, new JsonSerializerOptions { WriteIndented = true });

            var nodeInfoList = new List<NodeInfo>();

            foreach (var node in nodes.Items)
            {
                var nodeName = node.Metadata.Name;

                var cpuCores = node.Status.Capacity["cpu"];
                if (!int.TryParse(cpuCores.ToString(), out var parsedCpuCores))
                {
                    _logger.LogWarning("Failed to parse CPU cores for node: {NodeName}", nodeName);
                    parsedCpuCores = 0;
                }

                var memory = node.Status.Capacity["memory"];
                if (!long.TryParse(memory.ToString().Replace("Ki", ""), out var memKiB))
                {
                    _logger.LogWarning("Failed to parse memory for node: {NodeName}", nodeName);
                    memKiB = 0;
                }
                int physicalMemoryGB = (int) (memKiB / (1024.0 * 1024)); // Ki Converted to GB

                int diskCapacity = (int) (node.Status.Capacity.ContainsKey("ephemeral-storage")
                                && long.TryParse(node.Status.Capacity["ephemeral-storage"].ToString().Replace("Ki", ""), out var diskKiB)
                                ? diskKiB / (1024.0 * 1024)
                                : 0 );

                var conditions = node.Status.Conditions.ToDictionary(cond => cond.Type, cond => cond.Status);
                var diskPressure = conditions.ContainsKey("DiskPressure") ? conditions["DiskPressure"] : "Unknown";
                var memoryPressure = conditions.ContainsKey("MemoryPressure") ? conditions["MemoryPressure"] : "Unknown";
                var pidPressure = conditions.ContainsKey("PIDPressure") ? conditions["PIDPressure"] : "Unknown";
                var readyStatus = conditions.ContainsKey("Ready") ? conditions["Ready"] : "Unknown";


                var internalIP = node.Status.Addresses.FirstOrDefault(a => a.Type == "InternalIP")?.Address;
                if (string.IsNullOrEmpty(internalIP))
                {
                    _logger.LogWarning("No InternalIP found for node: {NodeName}", nodeName);
                    continue;
                }

                var instance = $"{internalIP}:9100";


                var cpuUsage = await _prometheusService.GetCpuUsageAsync(instance);
                var memoryUsage = await _prometheusService.GetMemoryUsageAsync(instance);
                var diskUsage = await _prometheusService.GetDiskUsageAsync(instance);

                _logger.LogInformation("Parsed Node Info - Name: {NodeName}, CPU Cores: {CpuCores}, Physical Memory: {MemoryGB} GB, Disk Capacity: {DiskGB} GB, CPU Usage: {CpuUsage}%, Memory Usage: {MemoryUsage}%, Disk Usage: {DiskUsage}%, DiskPressure: {DiskPressure}, MemoryPressure: {MemoryPressure}, PIDPressure: {PIDPressure}, ReadyStatus: {ReadyStatus}",
                                    nodeName, parsedCpuCores, physicalMemoryGB, diskCapacity, cpuUsage, memoryUsage, diskUsage, diskPressure, memoryPressure, pidPressure, readyStatus);

                nodeInfoList.Add(new NodeInfo
                {
                    NodeName = nodeName,
                    instanceName = instance,
                    CpuCores = parsedCpuCores,
                    PhysicalMemory = $"{physicalMemoryGB} GB",
                    DiskCapacity = $"{diskCapacity} GB",
                    CpuUsage = (int)Math.Round(cpuUsage),
                    MemoryUsage = (int)Math.Round(memoryUsage),
                    DiskUsage = (int)Math.Round(diskUsage),
                    DiskPressure = diskPressure,
                    MemoryPressure = memoryPressure,
                    PIDPressure = pidPressure,
                    ReadyStatus = readyStatus
                });
            }

            return nodeInfoList;
        }

        // Get all nodes actual CPU utilization
        public async Task<List<CpuActual>> GetCpuActualForInstance(string instanceName)
        {
            return await _prometheusService.GetCpuActualForInstance(instanceName);
        }

        // Get all nodes CPU forecast 
        public async Task<List<CpuForecast>> GetCpuForecastForAllNodesAsync()
        {
            return await _prometheusService.GetCpuForecastForAllNodesAsync();
        }

        // Query Cluster Status 
        public async Task<ClusterStatus> GetClusterStatusAsync()
        {
            return await _prometheusService.GetClusterStatusAsync();
        }

        // Query Cluster Utilization
        public async Task<ClusterUtilization> GetResourceUtilizationAsync()
        {
            var utilization = await _prometheusService.GetClusterUtilizationAsync();

            // Status
            utilization.CpuStatus = utilization.CpuUsage > 60 ? 1 : 0;
            utilization.MemoryStatus = utilization.RamUsage > 80 ? 1 : 0;
            utilization.DiskStatus = utilization.DiskUsage > 80 ? 1 : 0;

            return utilization;
        }

    }
}
