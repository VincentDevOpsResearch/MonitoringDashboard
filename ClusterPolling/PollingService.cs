#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using k8s;
using k8s.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json; 
using Polling.Models;

namespace Polling.Service
{
    public class PollingService
    {
        private readonly Kubernetes _client;
        private readonly ILogger<PollingService> _logger;

        public PollingService(ILogger<PollingService> logger)
        {
            _logger = logger;
            try
            {
                _client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
                _logger.LogInformation("Kubernetes client created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Kubernetes client: {ex.Message}");
                throw;
            }
        }

        public async Task QueryAndStoreMetricsAsync()
        {
            using var dbContext = new MonitoringDbContext();

            try
            {
                _logger.LogInformation("Fetching and storing cluster metrics...");
                
                // Get the metrics (node-level and cluster-level)
                var (nodeMetricsList, clusterMetric) = await GetClusterMetricsAsync();
                
                // Store cluster-level metrics
                if (clusterMetric != null && (clusterMetric.TotalNodes > 0 || clusterMetric.CpuUsage > 0 || clusterMetric.MemoryUsage > 0))
                {
                    dbContext.ClusterMetrics.Add(clusterMetric!);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Cluster metrics stored successfully.");
                }

                // Store node-level metrics
                if (nodeMetricsList.Any())
                {
                    dbContext.NodeMetrics.AddRange(nodeMetricsList!); // Add all node metrics in one go
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Node metrics stored successfully.");
                }


                 var podMetric = await GetPodMetricsAsync();

                // Save Pod Level Metrics
                if (podMetric != null)
                {
                    dbContext.PodMetrics.Add(podMetric!);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Pod metrics stored successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error storing metrics: {ex.Message}");
            }
        }


        private async Task<(List<NodeMetric>, ClusterMetric)> GetClusterMetricsAsync()
        {
            try
            {
                // Get Nodes Capacity from K8s API server
                var nodes = await _client.CoreV1.ListNodeAsync();
                int totalNodes = nodes.Items.Count;
                int activeNodes = nodes.Items.Count(n => n.Status.Conditions.Any(c => c.Type == "Ready" && c.Status == "True"));

                // Get Nodes Metrics (Current Usage) from metrics Server
                var rawMetrics = await _client.CustomObjects.ListClusterCustomObjectAsync(
                    "metrics.k8s.io", "v1beta1", "nodes");

                JsonElement json = (JsonElement)rawMetrics;

                if (json.TryGetProperty("items", out JsonElement items) && items.ValueKind == JsonValueKind.Array)
                {
                    double totalCpuUsage = 0;
                    double totalMemoryUsage = 0;
                    double totalCpuCores = 0;
                    double totalMemory = 0;
                    double totalDiskUsage = 0;
                    double totalDisk = 0;

                    List<NodeMetric> nodeMetricsList = new List<NodeMetric>();

                    foreach (var nodeMetrics in items.EnumerateArray())
                    {
                        var nodeName = nodeMetrics.GetProperty("metadata").GetProperty("name").GetString();
                        var usage = nodeMetrics.GetProperty("usage");

                        var cpuUsage = usage.GetProperty("cpu").GetString();
                        var memoryUsage = usage.GetProperty("memory").GetString();

                        double nodeCpuUsage = ParseCpu(cpuUsage);
                        double nodeMemoryUsage = ParseMemory(memoryUsage);
                        double nodeDiskUsage = 0;

                        var node = nodes.Items.FirstOrDefault(n => n.Metadata.Name == nodeName);
                        if (node != null)
                        {
                            double nodeCpuCores = double.Parse(node.Status.Capacity["cpu"].ToString());
                            double nodeMemory = ParseMemory(node.Status.Capacity["memory"].ToString());
                            double nodeAllocatableDisk = ParseMemory(node.Status.Allocatable["ephemeral-storage"].ToString());
                            double nodeCapacityDisk = ParseMemory(node.Status.Capacity["ephemeral-storage"].ToString());

                            totalCpuUsage += nodeCpuUsage;
                            totalMemoryUsage += nodeMemoryUsage;
                            totalCpuCores += nodeCpuCores;
                            totalMemory += nodeMemory;
                            totalDiskUsage += nodeCapacityDisk - nodeAllocatableDisk;
                            totalDisk += nodeCapacityDisk;

                            nodeCpuUsage = (nodeCpuUsage / nodeCpuCores) * 100;
                            nodeMemoryUsage = (nodeMemoryUsage / nodeMemory) * 100;
                            nodeDiskUsage = (nodeCapacityDisk - nodeAllocatableDisk ) / nodeCapacityDisk * 100;

                            nodeMetricsList.Add(new NodeMetric
                            {
                                NodeName = nodeName,
                                CpuUsage = nodeCpuUsage,
                                MemoryUsage = nodeMemoryUsage,
                                DiskUsage = nodeDiskUsage 
                            });
                        }
                    }

                    double cpuUsagePercentage = (totalCpuUsage / totalCpuCores) * 100;
                    double memoryUsagePercentage = (totalMemoryUsage / totalMemory) * 100;
                    double diskUsagePercentage = (totalDiskUsage / totalDisk) * 100;

                    var clusterMetric = new ClusterMetric
                    {
                        TotalNodes = totalNodes,
                        ActiveNodes = activeNodes,
                        CpuUsage = cpuUsagePercentage,
                        MemoryUsage = memoryUsagePercentage,
                        DiskUsage = diskUsagePercentage
                    };

                    return (nodeMetricsList, clusterMetric);
                }

                _logger.LogWarning("No node metrics found.");
                return (new List<NodeMetric>(), new ClusterMetric());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching cluster metrics: {ex.Message}");
                return (new List<NodeMetric>(), new ClusterMetric());
            }
        }

        public async Task<PodMetric> GetPodMetricsAsync()
        {
            try
            {
                var pods = await _client.CoreV1.ListNamespacedPodAsync("default");  
                int totalPods = pods.Items.Count;
                int runningPods = pods.Items.Count(pod => pod.Status.Phase == "Running");

                var podMetric = new PodMetric
                {
                    TotalPods = totalPods,
                    RunningPods = runningPods
                };

                return podMetric;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching pod metrics: {ex.Message}");
                return new PodMetric();
            }
        }

        private double ParseCpu(string cpuUsage)
        {
            if (string.IsNullOrEmpty(cpuUsage)) return 0;
            // Remove the "n" suffix and convert the remaining string to a number
            if (cpuUsage.EndsWith("n"))
            {
                cpuUsage = cpuUsage.Substring(0, cpuUsage.Length - 1); // Remove the "n"
            }
            return double.TryParse(cpuUsage, out double result) ? result / 1_000_000_000 : 0; // Convert to cores
        }

        private double ParseMemory(string memoryUsage)
        {
            if (string.IsNullOrEmpty(memoryUsage)) return 0;
            // If the memory usage ends with "Ki", convert to bytes by multiplying by 1024
            if (memoryUsage.EndsWith("Ki"))
            {
                memoryUsage = memoryUsage.Substring(0, memoryUsage.Length - 2); // Remove the "Ki"
                return double.TryParse(memoryUsage, out double result) ? result * 1024 : 0; // Return memory in bytes
            }
            // If the memory usage ends with "Mi", convert to bytes by multiplying by 1024 * 1024
            else if (memoryUsage.EndsWith("Mi"))
            {
                memoryUsage = memoryUsage.Substring(0, memoryUsage.Length - 2); // Remove the "Mi"
                return double.TryParse(memoryUsage, out double result) ? result * 1024 * 1024 : 0; // Return memory in bytes
            }
            // If the memory usage ends with "Gi", convert to bytes by multiplying by 1024 * 1024 * 1024
            else if (memoryUsage.EndsWith("Gi"))
            {
                memoryUsage = memoryUsage.Substring(0, memoryUsage.Length - 2); // Remove the "Gi"
                return double.TryParse(memoryUsage, out double result) ? result * 1024 * 1024 * 1024 : 0; // Return memory in bytes
            }

            // Handle cases for other units (like "Ti", "Pi" etc.), or just parse the number if no unit is specified
            return double.TryParse(memoryUsage, out double parsedResult) ? parsedResult : 0; // Return raw memory if no unit is present
        }


    }
}
