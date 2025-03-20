import React, { useEffect, useState } from "react";
import MetricCard from "../MetricCard";
import UtilizationChart from "./UtilizationChart";
import { Box, Typography, Paper, CircularProgress } from "@mui/material";
import { fetchData } from "../../api/requests";
import { CLUSTER_STATUS, CLUSTER_UTILIZATION, THRESHOLDS } from "../../api/constants";

interface PerformanceData {
  cpuUsage: number;
  diskUsage: number;
  ramUsage: number;
  cpuStatus: number;
  memoryStatus: number;
  diskStatus: number;
}

interface ClusterMetrics {
  totalNodes: number;
  activeNodes: number;
  totalPods: number;
  runningPods: number;
}

interface Thresholds {
  TotalNodes: { threshold: number; mode: number };
  ActiveNodes: { threshold: number; mode: number };
  TotalPods: { threshold: number; mode: number };
  RunningPods: { threshold: number; mode: number };
  CpuUtilization: { threshold: number; mode: number };
  MemoryUtilization: { threshold: number; mode: number };
  DiskUtilization: { threshold: number; mode: number };
}

const ClusterPerformance: React.FC = () => {
  const [clusterMetrics, setClusterMetrics] = useState<ClusterMetrics | null>(null);
  const [performanceData, setPerformanceData] = useState<PerformanceData | null>(null);
  const [thresholds, setThresholds] = useState<Thresholds | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchAllData = async () => {
      try {
        const [metricsData, utilizationData, thresholdsData] = await Promise.all([
          fetchData<ClusterMetrics>({ path: CLUSTER_STATUS }),
          fetchData<PerformanceData>({ path: CLUSTER_UTILIZATION }),
          fetchData<Thresholds>({ path: THRESHOLDS }),
        ]);

        setClusterMetrics(metricsData);
        setPerformanceData(utilizationData);
        setThresholds(thresholdsData);
      } catch (error) {
        console.error("Error fetching cluster data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchAllData();
    const interval = setInterval(fetchAllData, 60000);
    return () => clearInterval(interval);
  }, []);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="50vh">
        <CircularProgress />
      </Box>
    );
  }

  if (!clusterMetrics || !performanceData || !thresholds) {
    return (
      <Typography variant="h6" align="center" color="error">
        Failed to load data. Please try again later.
      </Typography>
    );
  }

  return (
    <div className="overall-cluster-performance">
      <Typography variant="h4" align="center" gutterBottom>
        Overall Cluster Performance
      </Typography>
      <div className="cluster-metrics">
        <MetricCard
          title="Total Nodes"
          value={clusterMetrics.totalNodes}
          alertThreshold={thresholds.TotalNodes.threshold}
          alertMode={thresholds.TotalNodes.mode}
          isAlertEnabled={true}
        />
        <MetricCard
          title="Active Nodes"
          value={clusterMetrics.activeNodes}
          alertThreshold={thresholds.ActiveNodes.threshold}
          alertMode={thresholds.ActiveNodes.mode}
          isAlertEnabled={true}
        />
        <MetricCard
          title="Total Pods"
          value={clusterMetrics.totalPods}
          alertThreshold={thresholds.TotalPods.threshold}
          alertMode={thresholds.TotalPods.mode}
          isAlertEnabled={true}
        />
        <MetricCard
          title="Running Pods"
          value={clusterMetrics.runningPods}
          alertThreshold={thresholds.RunningPods.threshold}
          alertMode={thresholds.RunningPods.mode}
          isAlertEnabled={true}
        />
      </div>
      <Paper elevation={3} style={{ borderRadius: "8px", position: "relative" }}>
        <Box className="utilization-charts" display="flex" justifyContent="space-between" mb={2}>
          <UtilizationChart
            title="CPU Utilization"
            value={performanceData.cpuUsage}
            alertThreshold={thresholds.CpuUtilization.threshold}
            alertMode={thresholds.CpuUtilization.mode}
          />
          <UtilizationChart
            title="Memory Utilization"
            value={performanceData.ramUsage}
            alertThreshold={thresholds.MemoryUtilization.threshold}
            alertMode={thresholds.MemoryUtilization.mode}
          />
          <UtilizationChart
            title="Disk Utilization"
            value={performanceData.diskUsage}
            alertThreshold={thresholds.DiskUtilization.threshold}
            alertMode={thresholds.DiskUtilization.mode}
          />
        </Box>
      </Paper>
    </div>
  );
};

export default ClusterPerformance;
