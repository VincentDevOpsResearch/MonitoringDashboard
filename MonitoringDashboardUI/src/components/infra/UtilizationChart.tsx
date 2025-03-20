import React, { useEffect, useRef, useState } from "react";
import * as echarts from "echarts";
import "echarts-liquidfill";
import { useTheme } from "@mui/material/styles";
import { Box, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, TextField, Button, Typography } from "@mui/material";
import { fetchData } from "../../api/requests";
import { UPDATE_THRESHOLDS } from "../../api/constants";
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";


interface UtilizationChartProps {
  title: string;
  value: number;
  alertThreshold?: number;
  alertMode?: number;
}

const categoryTitleMap: Record<string, string> = {
  "CPU Utilization": "CpuUtilization",
  "Memory Utilization": "MemoryUtilization",
  "Disk Utilization": "DiskUtilization",
};

const UtilizationChart: React.FC<UtilizationChartProps> = (
  { title, 
    value, 
    alertThreshold = 0, 
    alertMode = 0 }) => {
  const theme = useTheme();
  const chartRef = useRef<HTMLDivElement | null>(null);

  const [threshold, setThreshold] = useState(alertThreshold);
  const [tempThreshold, setTempThreshold] = useState(alertThreshold.toString());
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const isAlert = threshold > 0 && ((alertMode === 0 && value >= threshold) || (alertMode === 1 && value !== threshold));
  const fillColor = isAlert ? theme.palette.error.main : theme.palette.primary.main;

  useEffect(() => {
    if (chartRef.current) {
      const chart = echarts.init(chartRef.current);
      chart.setOption({
        series: [
          {
            type: "liquidFill",
            data: [value / 100],
            color: [fillColor],
            radius: "90%",
            label: {
              formatter: `${value}%`,
              fontSize: 24,
              fontWeight: "bold",
              color: isAlert ? "#fff" : "#000",
            },
            outline: {
              borderDistance: 0,
              itemStyle: {
                borderWidth: 3,
                borderColor: fillColor,
              },
            },
            backgroundStyle: {
              color: "#f5f5f5",
            },
            waveAnimation: true,
          },
        ],
      });

      return () => {
        chart.dispose();
      };
    }
  }, [value, fillColor]);

  const handleCardClick = () => {
    setIsDialogOpen(true); // No need for event.stopPropagation() here
  };
  
  const handleClose = () => {
    setIsDialogOpen(false);
    setTempThreshold(threshold.toString()); // Reset the input value
  };

  const handleSave = async () => {
    const newThreshold = Number(tempThreshold);
    const category = categoryTitleMap[title];

    try {
      await fetchData({
        path: UPDATE_THRESHOLDS,
        method: "POST",
        data: {
          category,
          threshold: newThreshold,
        },
      });

      setThreshold(newThreshold);
      setIsDialogOpen(false);

      // Show success notification
      toast.success("Threshold updated successfully!", { position: "top-right", autoClose: 3000 });
    } catch (error) {
      toast.error("Failed to update threshold!", { position: "top-right", autoClose: 3000 });
    }
  };

  const handleThresholdChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setTempThreshold(event.target.value);
  };

  return (
    <Box textAlign="center" margin="10px">
      <Typography fontWeight="bold" marginBottom="5px" color={fillColor}>
        {title}
      </Typography>
      <div 
        ref={chartRef} 
        style={{ width: 150, height: 150, margin: "0 auto", cursor: "pointer" }} 
        onClick={handleCardClick} // Now applied directly to the div, ensuring consistent behavior
      />
      <Dialog open={isDialogOpen} onClose={handleClose}>
        <DialogTitle>Set Alert Threshold</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Please enter the new threshold value for {title}.
          </DialogContentText>
          <TextField
            autoFocus
            margin="dense"
            label="Threshold"
            type="number"
            fullWidth
            value={tempThreshold}
            onChange={handleThresholdChange}
            InputProps={{
              inputProps: {
                min: 0,
              },
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>Cancel</Button>
          <Button onClick={handleSave}>Save</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default UtilizationChart;
