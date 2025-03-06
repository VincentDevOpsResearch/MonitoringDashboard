import React from 'react';
import { Container } from '@mui/material';
import MetricsSummary from '../components/api/MetricsSummary';
import MetricsTableSection from '../components/api/MetricsTableSession';
import MetricsChartSection from '../components/api/MetricsChartSection';

const ApiMetric: React.FC = () => {
  return (
    <Container style={{ marginTop: '20px' }}>
      <MetricsSummary />
      <MetricsChartSection initialTimeWindow="1d" initialStep="1h" />
      <MetricsTableSection />
    </Container>
  );
};

export default ApiMetric;
