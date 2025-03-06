import * as React from 'react';
import { Box } from '@mui/material';
import Dashboard from '../components/infra/Dashboard';

const Home: React.FC = () => (
  <Box style={{ width: '100%', maxWidth: '1200px', margin: '0 auto' }}>
    <Dashboard />
  </Box>
);

export default Home;
