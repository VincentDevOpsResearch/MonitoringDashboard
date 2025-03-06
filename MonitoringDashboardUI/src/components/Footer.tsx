import React from 'react';
import { Box, Container, Typography } from '@mui/material';

const Footer = () => {
  return (
    <Box 
      component="footer" 
      sx={{
        backgroundColor: 'white', 
        color: 'black', 
        padding: '10px 20px', 
        width: 'calc(100% - 250px)', 
        position: 'fixed', 
        bottom: 0, 
        left: 200,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'nowrap'
      }}
    >
      <Typography variant="body2" sx={{ whiteSpace: 'nowrap' }}>
        {new Date().getFullYear()} © SkyWorld.
      </Typography>
      <Typography variant="body2" sx={{ whiteSpace: 'nowrap', textAlign: 'right' }}>
        Powered by SkyWorld EBiz
      </Typography>
    </Box>
  );
};

export default Footer;
