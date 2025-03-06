import React from 'react';
import { Drawer, List, ListItem, ListItemIcon, ListItemText, Box, Typography } from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import AssessmentIcon from '@mui/icons-material/Assessment';
import TrackChangesIcon from '@mui/icons-material/TrackChanges';
import { Link } from 'react-router-dom';
import logo from '../assets/images/skyworld/skyworldicon.png'; 

const drawerWidth = 200;
const backgroundColor = '#2A3266';

const Sidebar: React.FC = () => (
  <Drawer
    variant="permanent"
    sx={{
      width: drawerWidth,
      flexShrink: 0,
      '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', backgroundColor, color: 'white' },
    }}
  >
    <Box display="flex" alignItems="center" justifyContent="center" py={2} sx={{ backgroundColor }}>
      <img src={logo} alt="Logo" style={{ height: 40 }} />
    </Box>
    <Typography variant="h6" align="center" gutterBottom>
      MENU
    </Typography>
    <List>
      <ListItem button component={Link} to="/">
        <ListItemIcon><HomeIcon sx={{ color: 'white' }} /></ListItemIcon>
        <ListItemText primary="HOME" />
      </ListItem>
      <ListItem button component={Link} to="/api-metric">
        <ListItemIcon><AssessmentIcon sx={{ color: 'white' }} /></ListItemIcon>
        <ListItemText primary="API METRIC" />
      </ListItem>
      <ListItem button component={Link} to="/message-tracking">
        <ListItemIcon><TrackChangesIcon sx={{ color: 'white' }} /></ListItemIcon>
        <ListItemText primary="MESSAGE TRACKING" />
      </ListItem>
      <ListItem button component={Link} to="/log">
        <ListItemIcon><TrackChangesIcon sx={{ color: 'white' }} /></ListItemIcon>
        <ListItemText primary="LOG" />
      </ListItem>
    </List>
  </Drawer>
);

export default Sidebar;
