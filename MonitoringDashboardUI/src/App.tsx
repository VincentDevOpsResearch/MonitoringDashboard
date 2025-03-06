import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import Footer from './components/Footer';
import ApiMetric from './pages/ApiMetric';
import Home from './pages/Home';
import MessageTracking from './pages/MessageTracking';
import { Box } from '@mui/material';
import LogsViewer from './pages/LogsViewer';

const App: React.FC = () => (
  <Router>
    <Box display="flex" minHeight="100vh">
      <Sidebar />
      <Box display="flex" flexDirection="column" flexGrow={1} minWidth={0}>
        <Header />
        <Box component="main" flexGrow={1} sx={{ p: 3, marginBottom: '50px', overflowY: 'auto' }}>
          <Routes>
            <Route path="/log" element={<LogsViewer />} />
            <Route path="/api-metric" element={<ApiMetric />} />
            <Route path="/message-tracking" element={<MessageTracking />} />
            <Route path="/" element={<Home />} />
          </Routes>
        </Box>
        <Footer />
      </Box>
    </Box>
  </Router>
);

export default App;
