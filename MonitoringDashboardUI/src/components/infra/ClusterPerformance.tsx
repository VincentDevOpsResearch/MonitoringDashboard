import React, { useEffect, useState } from "react";
import MetricCard from "../MetricCard";
import UtilizationChart from "./UtilizationChart";
import { Box, Typography, Paper } from "@mui/material";
import { fetchData } from '../../api/requests';
import { CLUSTER_STATUS, CLUSTER_UTILIZATION } from '../../api/constants'

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

const ClusterPerformance: React.FC = () => {
  const [clusterMetrics, setClusterMetrics] = useState({
    totalNodes: 0,
    activeNodes: 0,
    totalPods: 0,
    runningPods: 0,
  });

  const [performanceData, setPerformanceData] = useState({
    cpuUsage: 0,
    diskUsage: 0,
    ramUsage: 0,
    cpuStatus: 0,
    diskStatus: 0,
    memoryStatus: 0
  });

  // Fetch cluster status data
  useEffect(() => {
    const fetchClusterMetrics = async () => {
      try {
        const data = await fetchData<ClusterMetrics>({ path: CLUSTER_STATUS });

        setClusterMetrics({
          totalNodes: data.totalNodes,
          activeNodes: data.activeNodes,
          totalPods: data.totalPods,
          runningPods: data.runningPods,
        });
      } catch (error) {
        console.error("Error fetching cluster status:", error);
      }
    };

    fetchClusterMetrics();
    const interval = setInterval(fetchClusterMetrics, 60000); // Refresh every 5 minutes

    return () => clearInterval(interval);
  }, []);

  // Fetch cluster utilization data
  useEffect(() => {
    const fetchPerformanceData = async () => {
      try {

        const data = await fetchData<PerformanceData>({ path: CLUSTER_UTILIZATION });

        setPerformanceData({
          cpuUsage: data.cpuUsage,
          diskUsage: data.diskUsage,
          ramUsage: data.ramUsage,
          cpuStatus: data.cpuStatus,
          memoryStatus: data.memoryStatus,
          diskStatus: data.diskStatus,
        });
      } catch (error) {
        console.error('Error fetching utilization data:', error);
      }
    };

    fetchPerformanceData();
    const interval = setInterval(fetchPerformanceData, 300000); // Refresh every 5 minutes

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="overall-cluster-performance">
      <Typography variant="h4" align="center" gutterBottom>
        Overall Cluster Performance
      </Typography>
      <div className="cluster-metrics">
        <MetricCard title="Total Nodes" value={clusterMetrics.totalNodes} unit="" alertThreshold={10} />
        <MetricCard title="Active Nodes" value={clusterMetrics.activeNodes} unit="" alertThreshold={5} />
        <MetricCard title="Total Pods" value={clusterMetrics.totalPods} unit="" alertThreshold={5} />
        <MetricCard title="Running Pods" value={clusterMetrics.runningPods} unit="" alertThreshold={5} />
      </div>
      <Paper
        elevation={3}
        style={{borderRadius: "8px", position: "relative" }}
      >
        <Box className="utilization-charts" display="flex" justifyContent="space-between" mb={2}>
          <UtilizationChart title="CPU Utilization" value={performanceData.cpuUsage} status={performanceData.cpuStatus} />
          <UtilizationChart title="Memory Utilization" value={performanceData.ramUsage} status={performanceData.memoryStatus} />
          <UtilizationChart title="Disk Utilization" value={performanceData.diskUsage} status={performanceData.diskStatus} />
        </Box>
      </Paper>
    </div>
  );
};

export default ClusterPerformance;
