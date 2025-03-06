import React, { useEffect, useRef } from "react";
import * as echarts from "echarts";
import "echarts-liquidfill";
import { useTheme } from "@mui/material/styles";

interface UtilizationChartProps {
  title: string;
  value: number; 
  status: number; 
}

const UtilizationChart: React.FC<UtilizationChartProps> = ({ title, value, status }) => {
  const theme = useTheme();
  const chartRef = useRef<HTMLDivElement | null>(null);

  const fillColor = status === 1 ? theme.palette.error.main : theme.palette.primary.main;

  useEffect(() => {
    if (chartRef.current) {
      const chart = echarts.init(chartRef.current);
      chart.setOption({
        series: [
          {
            type: "liquidFill",
            data: [value / 100],
            color: [fillColor],
            radius: '90%',
            label: {
              formatter: `${value}%`,
              fontSize: 24,
              fontWeight: "bold",
              color: status === 1 ? "#fff" : "#000",
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
  }, [value, fillColor, status]);

  return (
    <div style={{ textAlign: "center", margin: "10px" }}>
      <p style={{ fontWeight: "bold", marginBottom: "5px", color: fillColor }}>{title}</p>
      <div ref={chartRef} style={{ width: 150, height: 150, margin: "0 auto" }} />
    </div>
  );
};

export default UtilizationChart;
