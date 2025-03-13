import React, { useEffect, useState } from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';
import ApiMetricTable from '../MetricTable';
import { fetchData } from '../../api/requests';
import { API_PERFORMANCE } from '../../api/constants'

interface ApiPerformanceMetrics {
  apiEndpoint: string;
  method: string;
  totalRequests: number;
  avgResponseTime: number;
  errorRate: number;
  minResponseTime: number;
  maxResponseTime: number;
  upperBoundResponseTime: number;
}

const MetricsTableSection: React.FC = () => {
  const [tableData, setTableData] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMetrics = async () => {
      try {
        setLoading(true);
        
        const data: ApiPerformanceMetrics[] = await fetchData({
          path: API_PERFORMANCE,
        });

        // Format data for table
        const formattedData = data.map((item) => ({
          request: `${item.method} ${item.apiEndpoint}`, // Combine method and endpoint
          totalRequests: item.totalRequests || 0,
          requestsPerHour: parseFloat((item.totalRequests / 24).toFixed(2)), // Calculate requests per hour
          avgRespTime: Math.round(item.avgResponseTime * 1000), // Convert to ms
          minRespTime: Math.round(item.minResponseTime * 1000),
          maxRespTime: Math.round(item.maxResponseTime * 1000),
          '90thRespTime': Math.round(item.upperBoundResponseTime * 1000),
          errorPercentage: parseFloat(item.errorRate.toFixed(2)), // Format error rate
        }));

        setTableData(formattedData);
      } catch (err: any) {
        setError(err.message || 'Failed to fetch metrics data');
      } finally {
        setLoading(false);
      }
    };

    fetchMetrics();
  }, []);

  if (loading) {
    return (
      <Box mt={3} display="flex" justifyContent="center">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box mt={3} textAlign="center">
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  return (
    <Box mt={3}>
      <ApiMetricTable data={tableData} />
    </Box>
  );
};

export default MetricsTableSection;
