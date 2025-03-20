import React, { useEffect } from 'react';
import { BrowserRouter as Router, Route, Routes, useLocation } from 'react-router-dom';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import Footer from './components/Footer';
import ApiMetric from './pages/ApiMetric';
import Home from './pages/Home';
import MessageTracking from './pages/MessageTracking';
import { Box } from '@mui/material';
import LogsViewer from './pages/LogsViewer';
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

// Function to set dynamic page titles based on the current route
const usePageTitle = () => {
  const location = useLocation();

  useEffect(() => {
    const pageTitles: Record<string, string> = {
      "/": "Home - Monitoring Dashboard",
      "/log": "Logs Viewer - Monitoring Dashboard",
      "/api-metric": "API Metrics - Monitoring Dashboard",
      "/message-tracking": "Message Tracking - Monitoring Dashboard",
    };

    document.title = pageTitles[location.pathname] || "Monitoring Dashboard";
  }, [location.pathname]);
};

const App: React.FC = () => (
  <Router>
    <Box display="flex" minHeight="100vh">
      <Sidebar />
      <Box display="flex" flexDirection="column" flexGrow={1} minWidth={0}>
        <Header />
        <Box component="main" flexGrow={1} sx={{ p: 3, marginBottom: '50px', overflowY: 'auto' }}>
          <Routes>
            <Route path="/log" element={<LogsViewerWrapper />} />
            <Route path="/api-metric" element={<ApiMetricWrapper />} />
            <Route path="/message-tracking" element={<MessageTrackingWrapper />} />
            <Route path="/" element={<HomeWrapper />} />
          </Routes>
        </Box>
        <Footer />
      </Box>
    </Box>

    <ToastContainer position="top-right" autoClose={3000} />
  </Router>
);

// Wrapped components to apply `usePageTitle`
const LogsViewerWrapper = () => { usePageTitle(); return <LogsViewer />; };
const ApiMetricWrapper = () => { usePageTitle(); return <ApiMetric />; };
const MessageTrackingWrapper = () => { usePageTitle(); return <MessageTracking />; };
const HomeWrapper = () => { usePageTitle(); return <Home />; };

export default App;
