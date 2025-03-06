import React, { useState, useEffect, useRef } from 'react';
import { fetchData } from '../api/requests'; // 假设 fetchData 支持分页查询
import { LOG_NAMESPACES, LOG_PODS, LOG_STREAM } from '../api/constants';
import {
  Box,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Button,
  CircularProgress,
  Typography,
  Paper,
} from '@mui/material';

interface LogEntry {
  timestamp: string;
  content: string;
}

const LogsViewer: React.FC = () => {
  const [namespaces, setNamespaces] = useState<string[]>([]);
  const [pods, setPods] = useState<string[]>([]);
  const [selectedNamespace, setSelectedNamespace] = useState<string>('');
  const [selectedPod, setSelectedPod] = useState<string>('');
  const [logs, setLogs] = useState<LogEntry[]>([]); // 这里定义为 LogEntry 类型的数组
  const [loadingNamespaces, setLoadingNamespaces] = useState<boolean>(false);
  const [loadingPods, setLoadingPods] = useState<boolean>(false);
  const [loadingLogs, setLoadingLogs] = useState<boolean>(false);
  const [hasMore, setHasMore] = useState<boolean>(false);
  const [nextStartTime, setNextStartTime] = useState<string | null>(null);

  const logContainerRef = useRef<HTMLDivElement>(null);

  // Scroll to bottom when logs update
  useEffect(() => {
    if (logContainerRef.current) {
      logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight;
    }
  }, [logs]);

  // Fetch Namespace list
  useEffect(() => {
    const fetchNamespaces = async () => {
      setLoadingNamespaces(true);
      try {
        const data = await fetchData<string[]>({
          path: LOG_NAMESPACES,
        });
        setNamespaces(data);
      } catch (error) {
        console.error('Error fetching namespaces:', error);
      } finally {
        setLoadingNamespaces(false);
      }
    };
    fetchNamespaces();
  }, []);

  // Fetch Pod list based on selected Namespace
  useEffect(() => {
    if (selectedNamespace) {
      const fetchPods = async () => {
        setLoadingPods(true);
        try {
          const data = await fetchData<string[]>({
            path: LOG_PODS,
            params: { namespaceName: selectedNamespace },
          });
          setPods(data);
        } catch (error) {
          console.error('Error fetching pods:', error);
        } finally {
          setLoadingPods(false);
        }
      };
      fetchPods();
    } else {
      setPods([]);
    }
  }, [selectedNamespace]);

  // Fetch logs with pagination
  const fetchLogs = async (loadMore = false) => {
    if (selectedNamespace && selectedPod) {
      setLoadingLogs(true);

      try {
        var params = {
          namespaceName: selectedNamespace,
          podName: selectedPod,
          maxLines: 1000, 
        };

        const { logs: newLogs, nextStartTime: newNextStartTime, hasMore: newHasMore } =
          await fetchData({
            path: LOG_STREAM,
            params,
          });

        if (loadMore) {
          setLogs((prevLogs) => [...prevLogs, ...newLogs]); 
        } else {
          setLogs(newLogs); 
        }

        setNextStartTime(newNextStartTime);
        setHasMore(newHasMore);
      } catch (error) {
        console.error('Error fetching logs:', error);
      } finally {
        setLoadingLogs(false);
      }
    }
  };

  // Cleanup on unmount
  useEffect(() => {
    setLogs([]);
    setHasMore(false);
    setNextStartTime(null);
  }, [selectedNamespace, selectedPod]);

  return (
    <Box display="flex" flexDirection="column" height="100vh">
      {/* Header */}
      <Box p={2} display="flex" alignItems="center" justifyContent="space-between">
        <Typography variant="h4">Logs Viewer</Typography>

        {/* Selectors in a row */}
        <Box display="flex" gap={2} alignItems="center">
          {/* Namespace Selector */}
          <FormControl sx={{ minWidth: 200 }} disabled={loadingNamespaces}>
            <InputLabel id="namespace-label">Namespace</InputLabel>
            <Select
              labelId="namespace-label"
              value={selectedNamespace}
              onChange={(e) => setSelectedNamespace(e.target.value)}
            >
              <MenuItem value="">
                {loadingNamespaces ? <CircularProgress size={24} /> : 'Select Namespace'}
              </MenuItem>
              {namespaces.map((namespace) => (
                <MenuItem key={namespace} value={namespace}>
                  {namespace}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Pod Selector */}
          <FormControl sx={{ minWidth: 200 }} disabled={!selectedNamespace || loadingPods}>
            <InputLabel id="pod-label">Pod</InputLabel>
            <Select
              labelId="pod-label"
              value={selectedPod}
              onChange={(e) => setSelectedPod(e.target.value)}
            >
              <MenuItem value="">
                {loadingPods ? <CircularProgress size={24} /> : 'Select Pod'}
              </MenuItem>
              {pods.map((pod) => (
                <MenuItem key={pod} value={pod}>
                  {pod}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Get Logs Button */}
          <Button
            variant="contained"
            color="primary"
            onClick={() => fetchLogs()}
            disabled={!selectedNamespace || !selectedPod || loadingLogs}
          >
            {loadingLogs ? <CircularProgress size={24} color="inherit" /> : 'Get Logs'}
          </Button>
        </Box>
      </Box>

      {/* Logs Display Area */}
      <Paper
        elevation={3}
        sx={{
          flex: 1,
          m: 2,
          p: 2,
          backgroundColor: '#f9f9f9',
          overflowY: 'auto',
        }}
      >
        <Typography variant="h6" gutterBottom>
          Logs
          {hasMore ? (
            <Button onClick={() => fetchLogs(true)} disabled={loadingLogs}>
              Load More
            </Button>
          ) : (
            <Typography component="span" color="textSecondary">
              {' '}
              (All Logs Loaded)
            </Typography>
          )}
        </Typography>
        <div
          ref={logContainerRef}
          style={{
            whiteSpace: 'pre-wrap',
            wordWrap: 'break-word',
            margin: 0,
            height: '100%',
            overflowY: 'auto',
          }}
        >
          {/* 遍历日志数组并显示每条日志的时间戳和内容 */}
          {logs.length > 0 ? (
            logs.map((log, index) => (
              <div key={index}>
                {log.timestamp}: {log.content}
              </div>
            ))
          ) : (
            <Typography>No logs available</Typography>
          )}
        </div>
      </Paper>
    </Box>
  );
};

export default LogsViewer;
