import React from 'react';
import { AppBar, Toolbar, Typography, IconButton, Avatar } from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';

const Header: React.FC = () => {
  return (
    <AppBar position="static" style={{ backgroundColor: '#b3e5fc' }}>
      <Toolbar>
        <IconButton edge="start" color="inherit" aria-label="menu">
          <MenuIcon />
        </IconButton>
        <Typography variant="h6" style={{ flexGrow: 1 }}>
          SkyWorld
        </Typography>
        <Avatar alt="User" src="../assets/images/skyworld/user_icon.png" />
      </Toolbar>
    </AppBar>
  );
}

export default Header;
