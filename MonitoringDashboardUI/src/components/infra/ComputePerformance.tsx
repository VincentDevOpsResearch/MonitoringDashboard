import React, { useState, useEffect } from "react";
import { Paper, Typography, Box, MenuItem, IconButton, CircularProgress } from "@mui/material";
import ArrowDropDownIcon from "@mui/icons-material/ArrowDropDown";
import Menu from "@mui/material/Menu";
import { Line } from "react-chartjs-2";
import SquareBarChart from "../SquareBarChart";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";
import { fetchData } from '../../api/requests';
import { NODES_INFO, NODES_CPU_ACTUAL } from '../../api/constants'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend);

interface CpuData {
  timestamp: string;
  value: number;
}

interface NodeInfo {
  nodeName: string;
  instanceName: string;
  cpuCores: number;
  physicalMemory: string;
  diskCapacity: string;
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  diskPressure: string;
  memoryPressure: string;
  pidPressure: string;
  readyStatus: string;
}

const ComputePerformance: React.FC = () => {
  const [nodes, setNodes] = useState<NodeInfo[]>([]);
  const [selectedNode, setSelectedNode] = useState<NodeInfo | null>(null);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [loading, setLoading] = useState(true);
  const [cpuData, setCpuData] = useState<number[]>([]);
  const [timeLabels, setTimeLabels] = useState<string[]>([]);

  useEffect(() => {
    const fetchNodes = async () => {
      try {
        const data = await fetchData<NodeInfo[]>({ path: NODES_INFO });
        
        setNodes(data);
        setSelectedNode(data[0]);
        setLoading(false);
      } catch (error) {
        console.error("Failed to fetch nodes:", error);
        setLoading(false);
      }
    };

    fetchNodes();
  }, []);

  useEffect(() => {
    const fetchCpuData = async () => {
      if (!selectedNode) return;
  
      try {
        const instanceName = selectedNode.instanceName;

        const data: CpuData[] = await fetchData({
          path: NODES_CPU_ACTUAL,
          params: { instanceName },
        });
  
        // console.log("Raw API response:", data);
  
        // Validate the data structure
        if (!Array.isArray(data) || !data.every(item => item.timestamp && item.value !== undefined)) {
          throw new Error("Invalid data structure received from API");
        }
  
        // Extract values and labels
        const values = data.map(item => item.value);
        const labels = data.map(item => {
          const timestamp = new Date(item.timestamp);
          return timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        });
  
        setCpuData(values); // Set the CPU utilization values
        setTimeLabels(labels); // Set the time labels
      } catch (error) {
        console.error("Failed to fetch CPU data:", error);
        setCpuData([]); // Fallback to empty data
        setTimeLabels([]); // Fallback to empty labels
      }
    };
  
    fetchCpuData();
  }, [selectedNode]);
  
  
  const handleNodeChange = (nodeName: string) => {
    const node = nodes.find((n) => n.nodeName === nodeName);
    if (node) {
      setSelectedNode(node);
    }
    setAnchorEl(null);
  };

  // Inside useEffect or relevant logic, ensure a default node is always selected
  useEffect(() => {
    if (nodes.length > 0 && !selectedNode) {
      setSelectedNode(nodes[0]); // Ensure default selection
    }
  }, [nodes]);

  const handleClick = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const chartData = {
    labels: timeLabels,
    datasets: [
      {
        label: "Actual CPU Utilization",
        data: cpuData,
        borderColor: "black",
        backgroundColor: "transparent",
        fill: false,
        tension: 0.1,
        borderWidth: 2,
      },
    ],
  };

  const options: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: "top" as const,
      },
      title: {
        display: true,
        text: "CPU Utilization Forecasting",
        font: {
          size: 18,
          weight: "bold",
        },
      },
    },
    scales: {
      x: {
        type: "category" as const,
      },
      y: {
        beginAtZero: true,
        max: 100,
      },
    },
  };

  if (loading) {
    return <CircularProgress />;
  }

  return (
    <Paper className="compute-performance">
      <Typography variant="h6" align="center" gutterBottom sx={{ fontWeight: "bold" }}>
        Compute Performance By Node
      </Typography>
      <Box className="performance-metrics">
        <div className="metric-item">
          <h4>
            {selectedNode?.nodeName}
            <IconButton onClick={handleClick}>
              <ArrowDropDownIcon />
            </IconButton>
            <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleClose}>
              {nodes.map((node) => (
                <MenuItem key={node.nodeName} onClick={() => handleNodeChange(node.nodeName)}>
                  {node.nodeName}
                </MenuItem>
              ))}
            </Menu>
          </h4>
        </div>
        <div className="utilization-metrics">
          <SquareBarChart
            title="CPU Utilization"
            value={selectedNode?.cpuUsage || 0}
            status={selectedNode?.pidPressure === "True" ? 1 : 0}
            subTitle= "CPU Cores"
            subValue= {selectedNode?.cpuCores !== undefined ? selectedNode.cpuCores.toString() : "N/A"} 
          />
          <SquareBarChart
            title="Memory Utilization"
            value={selectedNode?.memoryUsage || 0}
            status={selectedNode?.memoryPressure === "True" ? 1 : 0}
            subTitle= "Physical Memory"
            subValue= {selectedNode?.physicalMemory || ""} 
          />
          <SquareBarChart
            title="Disk Utilization"
            value={selectedNode?.diskUsage || 0}
            status={selectedNode?.diskPressure === "True" ? 1 : 0}
            subTitle= "Disk Capacity"
            subValue= {selectedNode?.diskCapacity || "" } 
          />
        </div>
      </Box>
      <Box width="100%" height="400px" mt={3}>
        <Line data={chartData} options={options} />
      </Box>
    </Paper>
  );
};

export default ComputePerformance;
