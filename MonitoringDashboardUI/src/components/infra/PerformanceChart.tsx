import React, { useState, useEffect } from "react";
import { Paper, Typography, Box, MenuItem, FormControl, InputLabel, Select } from "@mui/material";
import { Line } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from "chart.js";
import { fetchData } from "../../api/requests";
import { NODES_CPU_ACTUAL, NODES_CPU_FORECAST, NODES_MEMORY_ACTUAL, NODES_MEMORY_FORECAST } from "../../api/constants";

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler);

interface ActualData {
  timestamp: string;
  value: number;
}

interface ForecastData {
  timestamp: string;
  mean: number;
  lowerBound: number;
  upperBound: number;
}

interface PerformanceChartProps {
  nodeName: string;
}

const PerformanceChart: React.FC<PerformanceChartProps> = ({ nodeName }) => {
  const [selectedMetric, setSelectedMetric] = useState<"CPU" | "Memory">("CPU");
  const [actualData, setActualData] = useState<(number | null)[]>([]);
  const [forecastData, setForecastData] = useState<number[]>([]);
  const [upperBoundData, setUpperBoundData] = useState<number[]>([]);
  const [lowerBoundData, setLowerBoundData] = useState<number[]>([]);
  const [timeLabels, setTimeLabels] = useState<string[]>([]);

  useEffect(() => {
    const loadData = async () => {
      if (!nodeName) return;

      try {
        const actualPath = selectedMetric === "CPU" ? NODES_CPU_ACTUAL : NODES_MEMORY_ACTUAL;
        const forecastPath = selectedMetric === "CPU" ? NODES_CPU_FORECAST : NODES_MEMORY_FORECAST;

        const [actualResponse, forecastResponse] = await Promise.all([
          fetchData({ path: actualPath, params: { instanceName: nodeName } }),
          fetchData({ path: forecastPath, params: { instanceName: nodeName } })
        ]);

        // Sort data chronologically
        actualResponse.sort((a: ActualData, b: ActualData) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());
        forecastResponse.sort((a: ForecastData, b: ForecastData) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());

        // Adjust timestamps to Beijing Time (UTC+8) and format
        const labels = forecastResponse.map((item: ForecastData) => {
          const utcDate = new Date(item.timestamp);
          const beijingTime = new Date(utcDate.getTime() + 8 * 60 * 60 * 1000);
          return beijingTime.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
        });
        setTimeLabels(labels);

        // Align actual data timestamps to minute precision for accurate matching
        const toMinutePrecision = (timestamp: string) => {
          const date = new Date(timestamp);
          date.setSeconds(0, 0);
          return date.toISOString();
        };

        const actualMap = new Map(
          actualResponse.map((item: ActualData) => [toMinutePrecision(item.timestamp), item.value])
        );

        // Match actual data to forecast timestamps
        const alignedActualData = forecastResponse.map(
          (item: ForecastData) => actualMap.get(toMinutePrecision(item.timestamp)) ?? null
        );

        setActualData(alignedActualData);
        setForecastData(forecastResponse.map((item: ForecastData) => item.mean));
        setUpperBoundData(forecastResponse.map((item: ForecastData) => item.upperBound));
        setLowerBoundData(forecastResponse.map((item: ForecastData) => item.lowerBound));


      } catch (error) {
        console.error(`Failed to fetch ${selectedMetric} data:`, error);
        setActualData([]);
        setForecastData([]);
        setUpperBoundData([]);
        setLowerBoundData([]);
        setTimeLabels([]);
      }
    };

    loadData();

    //Automatically refresh data every minute
    const interval = setInterval(loadData, 60000)
    // Clear Interval on unmount
    return () => clearInterval(interval);
    
  }, [nodeName, selectedMetric]);

  const chartData = {
    labels: timeLabels,
    datasets: [
      {
        label: `Actual ${selectedMetric} Utilization`,
        data: actualData,
        borderColor: selectedMetric === "CPU" ? "black" : "green",
        backgroundColor: "transparent",
        fill: false,
        tension: 0.1,
        borderWidth: 2,
      },
      {
        label: `Predicted ${selectedMetric} Utilization`,
        data: forecastData,
        borderColor: selectedMetric === "CPU" ? "blue" : "purple",
        backgroundColor: "transparent",
        fill: false,
        borderDash: [5, 5],
        tension: 0.1,
        borderWidth: 2,
      },
      {
        label: "Prediction Range (Upper Bound)",
        data: upperBoundData,
        borderColor: "rgba(0, 123, 255, 0.2)",
        backgroundColor: "rgba(0, 123, 255, 0.2)",
        fill: "+1",
        borderWidth: 1,
      },
      {
        label: "Prediction Range (Lower Bound)",
        data: lowerBoundData,
        borderColor: "rgba(0, 123, 255, 0.2)",
        backgroundColor: "rgba(0, 123, 255, 0.2)",
        fill: "-1",
        borderWidth: 1,
      },
    ],
  };

  return (
    <Paper sx={{ padding: 2, marginTop: 2 }}>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", mb: 2 }}>
        <Typography variant="h6" sx={{ fontWeight: "bold", flexGrow: 1, textAlign: "center" }}>
          {selectedMetric} Utilization Forecast
        </Typography>
        <FormControl sx={{ minWidth: 140, ml: 2 }}>
          <InputLabel>Metric</InputLabel>
          <Select value={selectedMetric} onChange={(e) => setSelectedMetric(e.target.value as "CPU" | "Memory")}>
            <MenuItem value="CPU">CPU</MenuItem>
            <MenuItem value="Memory">Memory</MenuItem>
          </Select>
        </FormControl>
      </Box>
      <Box width="100%" height="400px">
        <Line data={chartData} options={{ responsive: true, maintainAspectRatio: false }} />
      </Box>
    </Paper>
  );
};

export default PerformanceChart;