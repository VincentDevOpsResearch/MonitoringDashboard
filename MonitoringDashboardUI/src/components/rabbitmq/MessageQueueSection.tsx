import React, { useEffect, useState } from 'react';
import { Box, CircularProgress } from '@mui/material';
import MessageQueueTable from '../MessageQueueTable';
import { fetchData } from '../../api/requests';
import { RABBITMQ_QUEUE } from '../../api/constants'; 

interface QueueData {
  virtualHost: string;
  name: string;
  type: string;
  state: string;
  readyMessages: number;
  unackedMessages: number;
  totalMessages: number;
  incomingRate: number;
  unackedRate: number;
}

const MessageQueueSection: React.FC = () => {
  const [tableData, setTableData] = useState<QueueData[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchQueueData = async () => {
      try {
        const data = await fetchData<QueueData[]>({ path: RABBITMQ_QUEUE });
        setTableData(data);
      } catch (err: any) {
        console.error('Failed to fetch queue data:', err);
        setError('Failed to load queue data.');
      } finally {
        setLoading(false);
      }
    };

    fetchQueueData();
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
      <Box mt={3} color="error.main" textAlign="center">
        {error}
      </Box>
    );
  }

  return (
    <Box mt={3}>
      <MessageQueueTable data={tableData} />
    </Box>
  );
};

export default MessageQueueSection;
