import React, { useState, useEffect } from 'react';
import {
  Container,
  Grid,
  Paper,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  OutlinedInput,
} from '@mui/material';
import { Line } from 'react-chartjs-2';
import { Chart, registerables } from 'chart.js';
import MetricCard from '../MetricCard';
import { SelectChangeEvent, Box } from '@mui/material';
import { fetchData } from '../../api/requests';
import { RABBITMQ_OVERVIEW } from '../../api/constants'; 

Chart.register(...registerables);

interface RabbitMQOverviewData {
    messageRateGraph: any[];
    queuedMessageGraph: any[];
    queues: number;
    consumers: number;
    channels: number;
    incomingRate: number;
    unacknowledged: number;
    queuedMessages: number;
}

interface GraphData {
    messageRateGraph: any[];
    queueMessageGraph: any[];
}
  

const timeRanges = [
  { label: 'Last Minute', lengthsAge: 60, lengthsIncr: 5, msgRates: 60, msgRatesIncr: 5 },
  { label: 'Last 10 Minutes', lengthsAge: 600, lengthsIncr: 5, msgRates: 600, msgRatesIncr: 5 },
  { label: 'Last Hour', lengthsAge: 3600, lengthsIncr: 60, msgRates: 3600, msgRatesIncr: 60 },
  { label: 'Last 8 Hours', lengthsAge: 28800, lengthsIncr: 600, msgRates: 28800, msgRatesIncr: 600 },
  { label: 'Last Day', lengthsAge: 86400, lengthsIncr: 1800, msgRates: 86400, msgRatesIncr: 1800 },
];

const OverviewSection = () => {
  const [selectedTimeRange, setSelectedTimeRange] = useState<number>(60);
  const [selectedMetric, setSelectedMetric] = useState<string>('messageRateGraph');
  const [graphData, setGraphData] = useState<GraphData>({
    messageRateGraph: [],
    queueMessageGraph: [],
  });
  
  const [currentGraphData, setCurrentGraphData] = useState({
    labels: [] as string[],
    datasets: [] as any[],
  });
  const [metrics, setMetrics] = useState([
    { title: 'Queues', value: 0 },
    { title: 'Consumers', value: 0 },
    { title: 'Channels', value: 0 },
    { title: 'Incoming', value: 0, unit: 'msg/s' },
    { title: 'UnAcknowledged', value: 0, unit: 'messages' },
    { title: 'Queued Messages', value: 0 },
  ]);

  const updateGraphData = (metric: string, graphData: any) => {
    if (metric === 'messageRateGraph') {
      setCurrentGraphData({
        labels: graphData.messageRateGraph.map((point: any) => point.timestamp),
        datasets: [
          {
            label: 'Message Rate',
            data: graphData.messageRateGraph.map((point: any) => point.sample),
            borderColor: 'blue',
            borderDash: [],
            fill: false,
          },
        ],
      });
    } else if (metric === 'queueMessageGraph') {
      setCurrentGraphData({
        labels: graphData.queuedMessageGraph.map((point: any) => point.timestamp),
        datasets: [
          {
            label: 'Queued Messages',
            data: graphData.queuedMessageGraph.map((point: any) => point.sample),
            borderColor: 'green',
            borderDash: [],
            fill: false,
          },
        ],
      });
    }
  };
  

  const fetchRabbitMQData = async ({
    lengthsAge,
    lengthsIncr,
    msgRates,
    msgRatesIncr,
  }: {
    lengthsAge?: number;
    lengthsIncr?: number;
    msgRates?: number;
    msgRatesIncr?: number;
  }) => {
    if (lengthsAge === undefined || msgRates === undefined) {
      console.warn('No valid parameters provided for fetchRabbitMQData. Skipping request.');
      return;
    }
  
    try {
      const params: Record<string, string> = {};
      if (lengthsAge !== undefined) params['lengths_age'] = lengthsAge.toString();
      if (lengthsIncr !== undefined) params['lengths_incr'] = lengthsIncr.toString();
      if (msgRates !== undefined) params['msg_rates'] = msgRates.toString();
      if (msgRatesIncr !== undefined) params['msg_rates_incr'] = msgRatesIncr.toString();
  
      const data = await fetchData<RabbitMQOverviewData>({
        path: RABBITMQ_OVERVIEW,
        params,
      });

      setGraphData({
        messageRateGraph: data.messageRateGraph ?? [],
        queueMessageGraph: data.queuedMessageGraph ?? [],
      });
  
      setMetrics([
        { title: 'Queues', value: data.queues ?? 0 },
        { title: 'Consumers', value: data.consumers ?? 0 },
        { title: 'Channels', value: data.channels ?? 0 },
        { title: 'Incoming', value: data.incomingRate ?? 0, unit: 'msg/s' },
        { title: 'UnAcknowledged', value: data.unacknowledged ?? 0, unit: 'messages' },
        { title: 'Queued Messages', value: data.queuedMessages ?? 0 },
      ]);
  
      if (selectedMetric) {
        console.log(selectedMetric, data)
        updateGraphData(selectedMetric, data);
      }
    } catch (error) {
      console.error('Error fetching RabbitMQ overview data:', error);
    }
  };
  

  useEffect(() => {
    const selectedRange = timeRanges.find((range) => range.lengthsAge === selectedTimeRange);
    if (selectedRange) {
      fetchRabbitMQData({
        lengthsAge: selectedRange.lengthsAge,
        lengthsIncr: selectedRange.lengthsIncr,
        msgRates: selectedRange.msgRates,
        msgRatesIncr: selectedRange.msgRatesIncr,
      });
    }
  }, [selectedTimeRange]);

  const handleTimeRangeChange = (event: SelectChangeEvent<number>) => {
    const value = Number(event.target.value);
    setSelectedTimeRange(value);

    const selectedRange = timeRanges.find((range) => range.lengthsAge === value);
    if (selectedRange) {
      if (selectedMetric === 'queueMessageGraph') {
        fetchRabbitMQData({
          lengthsAge: selectedRange.lengthsAge,
          lengthsIncr: selectedRange.lengthsIncr,
        });
      } else if (selectedMetric === 'messageRateGraph') {
        fetchRabbitMQData({
          msgRates: selectedRange.msgRates,
          msgRatesIncr: selectedRange.msgRatesIncr,
        });
      }
    }
  };

  const handleMetricChange = (event: SelectChangeEvent<string>) => {
    const metric = event.target.value as string;
    setSelectedMetric(metric);

    if (metric === 'messageRateGraph') {
      setCurrentGraphData({
        labels: graphData.messageRateGraph.map((point: any) => point.timestamp),
        datasets: [
          {
            label: 'Message Rate',
            data: graphData.messageRateGraph.map((point: any) => point.sample),
            borderColor: 'blue',
            borderDash: [],
            fill: false,
          },
        ],
      });
    } else if (metric === 'queueMessageGraph') {
      setCurrentGraphData({
        labels: graphData.queueMessageGraph.map((point: any) => point.timestamp),
        datasets: [
          {
            label: 'Queued Messages',
            data: graphData.queueMessageGraph.map((point: any) => point.sample),
            borderColor: 'green',
            borderDash: [],
            fill: false,
          },
        ],
      });
    }
  };

  return (
    <Container style={{ marginTop: '20px' }}>
      <Grid container spacing={3}>
        {metrics.map((metric, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <MetricCard title={metric.title} value={metric.value} unit={metric.unit} />
          </Grid>
        ))}
      </Grid>
      <Box display="flex" justifyContent="space-between" alignItems="center" style={{ marginTop: '20px' }}>
        <FormControl style={{ flex: 1, marginRight: '10px' }}>
            <InputLabel id="metric-select-label">Select Metric</InputLabel>
            <Select
            labelId="metric-select-label"
            value={selectedMetric}
            onChange={handleMetricChange}
            input={<OutlinedInput label="Select Metric" />}
            >
            <MenuItem value="messageRateGraph">Message Rate Graph</MenuItem>
            <MenuItem value="queueMessageGraph">Queued Message Graph</MenuItem>
            </Select>
        </FormControl>
        <FormControl style={{ flex: 1 }}>
            <InputLabel id="time-range-select-label">Select Time Range</InputLabel>
            <Select
            labelId="time-range-select-label"
            value={selectedTimeRange}
            onChange={handleTimeRangeChange}
            input={<OutlinedInput label="Select Time Range" />}
            >
            {timeRanges.map((range) => (
                <MenuItem key={range.lengthsAge} value={range.lengthsAge}>
                {range.label}
                </MenuItem>
            ))}
            </Select>
        </FormControl>
      </Box>

      <Paper elevation={3} style={{ marginTop: '20px', padding: '20px', height: '300px' }}>
        <Line
          data={currentGraphData}
          options={{
            maintainAspectRatio: false,
            scales: {
              y: {
                beginAtZero: true,
              },
            },
          }}
        />
      </Paper>
    </Container>
  );
};

export default OverviewSection;
