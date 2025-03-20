import React, { useEffect, useState } from "react";
import { Paper, Typography, Box, MenuItem, IconButton, CircularProgress } from "@mui/material";
import ArrowDropDownIcon from "@mui/icons-material/ArrowDropDown";
import Menu from "@mui/material/Menu";
import SquareBarChart from "../SquareBarChart";
import { fetchData } from '../../api/requests';
import PerformanceChart from "./PerformanceChart";
import { NODES_INFO } from '../../api/constants';

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

  const handleNodeChange = (nodeName: string) => {
    const node = nodes.find((n) => n.nodeName === nodeName);
    if (node) {
      setSelectedNode(node);
    }
    setAnchorEl(null);
  };

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
            subTitle="CPU Cores"
            subValue={selectedNode?.cpuCores !== undefined ? selectedNode.cpuCores.toString() : "N/A"} 
          />
          <SquareBarChart
            title="Memory Utilization"
            value={selectedNode?.memoryUsage || 0}
            status={selectedNode?.memoryPressure === "True" ? 1 : 0}
            subTitle="Physical Memory"
            subValue={selectedNode?.physicalMemory || ""} 
          />
          <SquareBarChart
            title="Disk Utilization"
            value={selectedNode?.diskUsage || 0}
            status={selectedNode?.diskPressure === "True" ? 1 : 0}
            subTitle="Disk Capacity"
            subValue={selectedNode?.diskCapacity || ""} 
          />
        </div>
      </Box>
      {selectedNode && <PerformanceChart nodeName={selectedNode.nodeName} />}
    </Paper>
  );
};

export default ComputePerformance;
