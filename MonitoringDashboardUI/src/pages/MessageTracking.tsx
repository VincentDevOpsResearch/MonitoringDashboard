import React from 'react';
import { Container } from '@mui/material';
import OverviewSection from '../components/rabbitmq/OverviewSection';
import MessageQueueSection from '../components/rabbitmq/MessageQueueSection';

const MessageTracking: React.FC = () => {
  return (
    <Container style={{ marginTop: '20px' }}>
      <OverviewSection />
      <MessageQueueSection />
    </Container>
  );
};

export default MessageTracking;
