import React from 'react';
import { BarChart, Bar, Cell, CartesianGrid, XAxis, YAxis, ResponsiveContainer } from 'recharts';
import { useTheme } from '@mui/material/styles';
import './SquareBarChart.css';

interface SquareBarChartProps {
  title: string;
  value: number;
  status: number;
  subTitle: string;
  subValue: string;
}

const SquareBarChart: React.FC<SquareBarChartProps> = ({ title, value, status, subTitle, subValue }) => {
  const theme = useTheme();

  const data = [{ name: title, value }];
  const fillColor = status === 1 ? theme.palette.error.main : theme.palette.primary.main;
  const textColor = status === 1 ? '#fff' : '#000';

  return (
    <div className="square-bar-chart-container">
      <div className="chart-title">
        <strong>{title}</strong>
      </div>
      <div className="chart-content">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart
            data={data}
            margin={{ top: 20, right: 20, left: 20, bottom: 20 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis type="category" dataKey="name" hide />
            <YAxis type="number" domain={[0, 100]} hide />
            <Bar dataKey="value" fill={fillColor} barSize={200}>
              <Cell fill={fillColor} />
            </Bar>
          </BarChart>
        </ResponsiveContainer>
        <div className="chart-value" style={{ color: textColor }}>{`${value}%`}</div>
      </div>
      <div className="chart-subinfo">
        <strong>{subTitle}</strong>
        <p>{subValue}</p>
      </div>
    </div>
  );
};

export default SquareBarChart;
