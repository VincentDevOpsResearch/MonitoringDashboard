import React, { useRef, useState, useEffect } from 'react';
import {
  FormControl,
  InputLabel,
  MenuItem,
  OutlinedInput,
  Select,
  Paper,
  CircularProgress,
  TextField,
  Grid,
} from '@mui/material';
import { Line } from 'react-chartjs-2';
import { ChartJSOrUndefined } from 'react-chartjs-2/dist/types';
import { fetchData } from '../../api/requests';
import { REQUESTS_SERIES, ERROR_RATE_SERIES, RESPONSE_TIME_SERIES } from '../../api/constants';

interface MetricsChartSectionProps {
  initialTimeWindow: string;
  initialStep: string;
}

const MetricsChartSection: React.FC<MetricsChartSectionProps> = ({
  initialTimeWindow,
  initialStep,
}) => {
  const chartRef = useRef<ChartJSOrUndefined<'line', number[], unknown>>(null);

  const availableMetrics = ['Requests', 'Error Rate', 'Average Response Time'];

  const [selectedMetric, setSelectedMetric] = useState<string>(availableMetrics[0]);
  const [chartData, setChartData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  // State for timeWindow and step inputs
  const [timeWindow, setTimeWindow] = useState<string>(initialTimeWindow);
  const [step, setStep] = useState<string>(initialStep);

  const handleMetricChange = (event: any) => {
    setSelectedMetric(event.target.value);
  };

  const handleTimeWindowChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setTimeWindow(event.target.value);
  };

  const handleStepChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setStep(event.target.value);
  };

  interface ChartData {
    labels: string[];
    datasets: {
      label: string;
      data: number[];
      borderColor: string;
      fill: boolean;
    }[];
  }
  
  const fetchChartData = async (metric: string): Promise<ChartData> => {

    const endpoints: Record<string, string> = {
      Requests: REQUESTS_SERIES,
      'Error Rate': ERROR_RATE_SERIES,
      'Average Response Time': RESPONSE_TIME_SERIES,
    };
  
    const endpoint = endpoints[metric];
    if (!endpoint) {
      throw new Error(`Unknown metric: ${metric}`);
    }
  
    try {
      const data = await fetchData<{ data: { timestamps: string[]; values: number[] } }>({
        path: endpoint,
        params: { timeWindow, step }, 
      });
  
      return {
        labels: data.data.timestamps.map((ts: string) => {
          const date = new Date(ts);
          return step.includes('h')
            ? `${date.getHours()}:00`
            : date.toLocaleTimeString();
        }),
        datasets: [
          {
            label: metric,
            data: data.data.values.map((v: number) => parseFloat(v.toFixed(2))),
            borderColor: metric === 'Requests' ? 'blue' : metric === 'Error Rate' ? 'red' : 'green',
            fill: false,
          },
        ],
      };
    } catch (error) {
      console.error(`Failed to fetch data for metric "${metric}":`, error);
      throw error;
    }
  };

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const data = await fetchChartData(selectedMetric);
        setChartData(data);
      } catch (error) {
        console.error('Error loading chart data:', error);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [selectedMetric, timeWindow, step]);

  return (
    <div>
      <Grid container spacing={2} style={{ marginTop: '20px' }}>
        <Grid item xs={12} md={4}>
          <FormControl fullWidth>
            <InputLabel id="metric-select-label">Select Metric</InputLabel>
            <Select
              labelId="metric-select-label"
              value={selectedMetric}
              onChange={handleMetricChange}
              input={<OutlinedInput label="Select Metric" />}
            >
              {availableMetrics.map((metric, index) => (
                <MenuItem key={index} value={metric}>
                  {metric}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField
            label="Time Window (e.g., 1h, 30m)"
            value={timeWindow}
            onChange={handleTimeWindowChange}
            fullWidth
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField
            label="Step (e.g., 5m, 1h)"
            value={step}
            onChange={handleStepChange}
            fullWidth
          />
        </Grid>
      </Grid>

      <Paper
        elevation={3}
        style={{
          marginTop: '20px',
          height: '300px',
          width: '100%', 
        }}
      >
        {loading ? (
          <CircularProgress />
        ) : (
          <div style={{ height: '100%', width: '100%' }}>
            <Line
              data={chartData || { labels: [], datasets: [] }}
              ref={chartRef}
              options={{
                maintainAspectRatio: false,
                responsive: true,
              }}
            />
          </div>
        )}
      </Paper>
    </div>
  );
};

export default MetricsChartSection;
