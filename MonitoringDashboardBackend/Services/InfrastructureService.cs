using k8s;
using System.Text.Json;
using MonitoringDashboard.Models;
using MonitoringDashboard.Data;
using k8s.KubeConfigModels;
using Microsoft.EntityFrameworkCore;

namespace MonitoringDashboard.Services
{
    public class InfrastructureService
    {
        private readonly PrometheusService _prometheusService;
        private readonly Kubernetes _client;
        private readonly ILogger<InfrastructureService> _logger;

        private readonly MonitoringDbContext _dbContext;

        public InfrastructureService(PrometheusService prometheusService, ILogger<InfrastructureService> logger, MonitoringDbContext dbContext)
        {
            _prometheusService = prometheusService;
            _logger = logger;
            _dbContext = dbContext;

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

                var latestNodeMetric = await _dbContext.NodeMetrics
                    .Where(n => n.NodeName == nodeName)
                    .OrderByDescending(n => n.Timestamp)
                    .FirstOrDefaultAsync(); 

                if (latestNodeMetric == null)
                {
                    _logger.LogWarning("No utilization data found for node: {NodeName}. Returning default values.", nodeName);
                    nodeInfoList.Add(new NodeInfo
                    {
                        NodeName = nodeName,
                        CpuCores = 0,
                        PhysicalMemory = $"{physicalMemoryGB} GB",
                        DiskCapacity = $"{diskCapacity} GB",
                        CpuUsage =  0,
                        MemoryUsage = 0,
                        DiskUsage = 0,
                        DiskPressure = diskPressure,
                        MemoryPressure = memoryPressure,
                        PIDPressure = pidPressure,
                        ReadyStatus = readyStatus
                    });
                }
                else {
                    var cpuUsage = latestNodeMetric.CpuUsage;
                    var memoryUsage = latestNodeMetric.MemoryUsage;
                    var diskUsage = latestNodeMetric.DiskUsage;

                    _logger.LogInformation("Parsed Node Info - Name: {NodeName}, CPU Cores: {CpuCores}, Physical Memory: {MemoryGB} GB, Disk Capacity: {DiskGB} GB, CPU Usage: {CpuUsage}%, Memory Usage: {MemoryUsage}%, Disk Usage: {DiskUsage}%, DiskPressure: {DiskPressure}, MemoryPressure: {MemoryPressure}, PIDPressure: {PIDPressure}, ReadyStatus: {ReadyStatus}",
                                        nodeName, parsedCpuCores, physicalMemoryGB, diskCapacity, cpuUsage, memoryUsage, diskUsage, diskPressure, memoryPressure, pidPressure, readyStatus);

                    nodeInfoList.Add(new NodeInfo
                    {
                        NodeName = nodeName,
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
            }

            return nodeInfoList;
        }

        /// <summary>
        /// Fetches the latest 60 CPU usage records for a specific instance and computes 12 averaged segments (5-minute intervals).
        /// </summary>
        public async Task<List<CpuData>> GetCpuActualForInstance(string instanceName)
        {
            // Get the last two forecast timestamps
            var lastTwoForecasts = await _dbContext.CpuForecasts
                .Where(f => f.ItemId == instanceName+"_cpu")
                .OrderByDescending(f => f.Timestamp)
                .Take(2) // Get the most recent two forecasts
                .ToListAsync();

            List<NodeMetric> rawActualData;

            // If there are no forecasts, return an empty list (we cannot determine the actual data range)
            if (lastTwoForecasts.Count < 2)
            {
                rawActualData = await _dbContext.NodeMetrics
                    .Where(n => n.NodeName == instanceName)
                    .OrderByDescending(n => n.Timestamp)
                    .Take(60)
                    .ToListAsync();
            }
            else{
                var lastTime = lastTwoForecasts[1].Timestamp;
                var oneHourBefore = lastTime.AddHours(-1);

               _logger.LogInformation("Forecast alignment for {InstanceName}: lastTime = {LastTime}, oneHourBefore = {OneHourBefore}", 
            instanceName, lastTime, oneHourBefore);

            // Fetch actual CPU usage within this 1-hour window
                rawActualData = await _dbContext.NodeMetrics
                    .Where(n => n.NodeName == instanceName && n.Timestamp >= oneHourBefore && n.Timestamp <= lastTime)
                    .OrderBy(n => n.Timestamp) // Ensure chronological order
                    .ToListAsync();
            } 

            rawActualData.Reverse();

            var groupedData = rawActualData
                .Select(n => new
                {
                    RoundedTimestamp = new DateTime(n.Timestamp.Year, n.Timestamp.Month, n.Timestamp.Day, 
                                                    n.Timestamp.Hour, n.Timestamp.Minute / 5 * 5, 0), // Round to nearest 5-minute mark
                    n.CpuUsage
                })
                .GroupBy(g => g.RoundedTimestamp)
                .Select(g => new CpuData
                {
                    Timestamp = g.Key,  // Use the rounded 5-minute timestamp
                    Value = g.Average(d => d.CpuUsage)  // Compute average CPU usage
                })
                .OrderBy(d => d.Timestamp) // Ensure final data is ordered by time
                .ToList();

            return groupedData;
        }

        /// <summary>
        /// Fetches the latest 60 memory usage records for a specific instance and computes 12 averaged segments (5-minute intervals).
        /// </summary>
        public async Task<List<MemoryData>> GetMemoryActualForInstance(string instanceName)
        {
            // Get the last two forecast timestamps
            var lastTwoForecasts = await _dbContext.MemoryForecasts
                .Where(f => f.ItemId == instanceName+"_memory")
                .OrderByDescending(f => f.Timestamp)
                .Take(2) // Get the most recent two forecasts
                .ToListAsync();

            List<NodeMetric> rawActualData;

            // If there are no forecasts, return an empty list (we cannot determine the actual data range)
            if (lastTwoForecasts.Count < 2)
            {
                rawActualData = await _dbContext.NodeMetrics
                    .Where(n => n.NodeName == instanceName)
                    .OrderByDescending(n => n.Timestamp)
                    .Take(60)
                    .ToListAsync();
            }
            else{
                var lastTime = lastTwoForecasts[1].Timestamp;
                var oneHourBefore = lastTime.AddHours(-1);

               _logger.LogInformation("Forecast alignment for {InstanceName}: lastTime = {LastTime}, oneHourBefore = {OneHourBefore}", 
            instanceName, lastTime, oneHourBefore);

            // Fetch actual CPU usage within this 1-hour window
                rawActualData = await _dbContext.NodeMetrics
                    .Where(n => n.NodeName == instanceName && n.Timestamp >= oneHourBefore && n.Timestamp <= lastTime)
                    .OrderBy(n => n.Timestamp) // Ensure chronological order
                    .ToListAsync();
            } 

            rawActualData.Reverse();

            var groupedData = rawActualData
                .Select(n => new
                {
                    RoundedTimestamp = new DateTime(n.Timestamp.Year, n.Timestamp.Month, n.Timestamp.Day, 
                                                    n.Timestamp.Hour, n.Timestamp.Minute / 5 * 5, 0), // Round to nearest 5-minute mark
                    n.MemoryUsage
                })
                .GroupBy(g => g.RoundedTimestamp)
                .Select(g => new MemoryData
                {
                    Timestamp = g.Key,  // Use the rounded 5-minute timestamp
                    Value = g.Average(d => d.MemoryUsage)  // Compute average CPU usage
                })
                .OrderBy(d => d.Timestamp) // Ensure final data is ordered by time
                .ToList();

            return groupedData;
        }


        public async Task<List<CpuForecastData>> GetCpuForecastForInstance(string instanceName)
        {
            // Fetch the latest 13 forecast records for the given instance
            var forecastData = await _dbContext.CpuForecasts
                .Where(f => f.ItemId == instanceName + "_cpu")
                .OrderByDescending(f => f.Timestamp)
                .Take(13)
                .Select(f => new CpuForecastData
                {
                    Timestamp = f.Timestamp,
                    Mean = f.Mean,
                    LowerBound = f.LowerBound,
                    UpperBound = f.UpperBound
                })
                .ToArrayAsync(); // Directly convert to array

            // Return forecast data sorted in chronological order (oldest first)
            return forecastData.Reverse().ToList();
        }

        public async Task<List<MemoryForecastData>> GetMemoryForecastForInstance(string instanceName)
        {
            // Fetch the latest 13 forecast records for the given instance
            var forecastData = await _dbContext.MemoryForecasts
                .Where(f => f.ItemId == instanceName + "_memory")
                .OrderByDescending(f => f.Timestamp)
                .Take(13)  // 12 interval + 1 more forecast
                .Select(f => new MemoryForecastData
                {
                    Timestamp = f.Timestamp,
                    Mean = f.Mean,
                    LowerBound = f.LowerBound,
                    UpperBound = f.UpperBound
                })
                .ToArrayAsync(); // Directly convert to array

            // Return forecast data sorted in chronological order (oldest first)
            return forecastData.Reverse().ToList();
        }


        public async Task<ClusterStatus> GetClusterStatusAsync()
        {
            _logger.LogInformation("Fetching latest cluster status from the database.");

            // Fetch the latest cluster metric (contains TotalNodes, ActiveNodes, CPU, Memory, Disk usage)
            var latestClusterMetric = await _dbContext.ClusterMetrics
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync(); // Get only the latest record

            // Fetch the latest pod metric (contains TotalPods, RunningPods)
            var latestPodMetric = await _dbContext.PodMetrics
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync(); // Get only the latest record

            // Construct the cluster status response
            var clusterStatus = new ClusterStatus
            {
                TotalNodes = latestClusterMetric?.TotalNodes ?? 0, // Use latest cluster record or default to 0
                ActiveNodes = latestClusterMetric?.ActiveNodes ?? 0,
                TotalPods = latestPodMetric?.TotalPods ?? 0,
                RunningPods = latestPodMetric?.RunningPods ?? 0
            };

            _logger.LogInformation("Cluster status: TotalNodes={TotalNodes}, ActiveNodes={ActiveNodes}, TotalPods={TotalPods}, RunningPods={RunningPods}",
                clusterStatus.TotalNodes, clusterStatus.ActiveNodes, clusterStatus.TotalPods, clusterStatus.RunningPods);

            return clusterStatus;
        }



        // Query Cluster Utilization
        public async Task<ClusterUtilization> GetResourceUtilizationAsync()
        {
            _logger.LogInformation("Fetching latest cluster utilization from the database.");

            // Fetch the latest cluster utilization data from ClusterMetric table
            var latestClusterMetric = await _dbContext.ClusterMetrics
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync(); // Get only the latest record

            if (latestClusterMetric == null)
            {
                _logger.LogWarning("No cluster utilization data found. Returning default values.");
                return new ClusterUtilization
                {
                    CpuUsage = 0,
                    DiskUsage = 0,
                    RamUsage = 0,
                    CpuStatus = 0,
                    MemoryStatus = 0,
                    DiskStatus = 0
                };
            }

            // Convert usage values to integers and apply rounding
            var clusterUtilization = new ClusterUtilization
            {
                CpuUsage = (int)Math.Round(latestClusterMetric.CpuUsage), // Convert CPU usage to percentage
                DiskUsage = (int)Math.Round(latestClusterMetric.DiskUsage ?? 0), // Disk usage might be null
                RamUsage = (int)Math.Round(latestClusterMetric.MemoryUsage), // Memory usage as integer

                // Set status based on thresholds
                CpuStatus = latestClusterMetric.CpuUsage * 100 > 60 ? 1 : 0,
                MemoryStatus = latestClusterMetric.MemoryUsage > 80 ? 1 : 0,
                DiskStatus = (latestClusterMetric.DiskUsage ?? 0) > 80 ? 1 : 0
            };

            return clusterUtilization;

        }

    }
}
