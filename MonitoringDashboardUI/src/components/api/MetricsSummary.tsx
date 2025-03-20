import React, { useEffect, useState } from 'react';
import { Grid, CircularProgress } from '@mui/material';
import MetricCard from '../MetricCard';
import { fetchData } from '../../api/requests';
import { REQUESTS, ERROR_RATE, RESPONSE_TIME, THRESHOLDS } from '../../api/constants';

interface Thresholds {
  RequestsLast24Hours?: { threshold: number; mode: number };
  RequestsThisHour?: { threshold: number; mode: number };
  AvgResponseLast24Hours?: { threshold: number; mode: number };
  AvgResponseThisHour?: { threshold: number; mode: number };
  ErrorRateLast24Hours?: { threshold: number; mode: number };
  ErrorRateThisHour?: { threshold: number; mode: number };
}

const MetricsSummary: React.FC = () => {
  const [metrics, setMetrics] = useState([
    { title: 'Requests Last 24 Hours', value: 0, unit: 'requests' },
    { title: 'Avg. Response Last 24 Hours', value: 0, unit: 'ms' },
    { title: 'Error Rate Last 24 Hours', value: 0, unit: '%' },
    { title: 'Requests This Hour', value: 0, unit: 'requests' },
    { title: 'Avg. Response This Hour', value: 0, unit: 'ms' },
    { title: 'Error Rate This Hour', value: 0, unit: '%' },
  ]);

  const [thresholds, setThresholds] = useState<Thresholds | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchMetrics = async () => {
    try {
      const [
        requests24hResponse,
        requests1hResponse,
        errorRate24hResponse,
        errorRate1hResponse,
        responseTime24hResponse,
        responseTime1hResponse,
      ] = await Promise.all([
        fetchData<{ data: any }>({ path: REQUESTS, params: { timeWindow: '1d' } }),
        fetchData<{ data: any }>({ path: REQUESTS, params: { timeWindow: '1h' } }),
        fetchData<{ data: any }>({ path: ERROR_RATE, params: { timeWindow: '1d' } }),
        fetchData<{ data: any }>({ path: ERROR_RATE, params: { timeWindow: '1h' } }),
        fetchData<{ data: any }>({ path: RESPONSE_TIME, params: { timeWindow: '1d' } }),
        fetchData<{ data: any }>({ path: RESPONSE_TIME, params: { timeWindow: '1h' } }),
      ]);

      return {
        requests24h: requests24hResponse.data,
        requests1h: requests1hResponse.data,
        errorRate24h: errorRate24hResponse.data,
        errorRate1h: errorRate1hResponse.data,
        responseTime24h: responseTime24hResponse.data,
        responseTime1h: responseTime1hResponse.data,
      };
    } catch (error) {
      console.error('Failed to fetch metrics:', error);
      throw error;
    }
  };

  const fetchThresholds = async () => {
    try {
      const data = await fetchData<Thresholds>({ path: THRESHOLDS });
      setThresholds(data);
    } catch (error) {
      console.error('Failed to fetch thresholds:', error);
    }
  };

  useEffect(() => {
    const loadMetrics = async () => {
      setLoading(true);
      try {
        const [data] = await Promise.all([fetchMetrics(), fetchThresholds()]);

        setMetrics([
          { title: 'Requests Last 24 Hours', value: Math.round(data.requests24h), unit: 'requests' },
          { title: 'Avg. Response Last 24 Hours', value: data.responseTime24h, unit: 'ms' },
          { title: 'Error Rate Last 24 Hours', value: data.errorRate24h, unit: '%' },
          { title: 'Requests This Hour', value: Math.round(data.requests1h), unit: 'requests' },
          { title: 'Avg. Response This Hour', value: data.responseTime1h, unit: 'ms' },
          { title: 'Error Rate This Hour', value: data.errorRate1h, unit: '%' },
        ]);
      } catch (error) {
        console.error('Error loading metrics summary:', error);
      } finally {
        setLoading(false);
      }
    };

    loadMetrics();
    const interval = setInterval(loadMetrics, 60000); // Refresh data every minute

    return () => clearInterval(interval);
  }, []);

  if (loading || !thresholds) {
    return (
      <Grid container justifyContent="center">
        <CircularProgress />
      </Grid>
    );
  }

  return (
    <Grid container spacing={3}>
      {metrics.map((metric) => {
        const key = metric.title.replace(/\s+/g, '') as keyof Thresholds; // Ensure TypeScript allows dynamic lookup
        return (
          <Grid item xs={12} sm={6} md={4} key={metric.title}>
            <MetricCard
              title={metric.title}
              value={metric.value}
              unit={metric.unit}
              alertThreshold={thresholds[key]?.threshold ?? 0} // Safe lookup
              alertMode={thresholds[key]?.mode ?? 0} // Safe lookup
              isAlertEnabled={true}
            />
          </Grid>
        );
      })}
    </Grid>
  );
};

export default MetricsSummary;
