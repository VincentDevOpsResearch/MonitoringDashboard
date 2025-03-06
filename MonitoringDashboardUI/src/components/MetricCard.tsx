import React, { useState } from 'react';
import { Paper, Typography, Box, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, TextField, IconButton, Button } from '@mui/material';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import NotificationsOffIcon from '@mui/icons-material/NotificationsOff';

interface MetricCardProps {
  title: string;
  value: number;
  unit?: string;
  alertThreshold?: number;
}

const MetricCard: React.FC<MetricCardProps> = ({ title, value, unit = '', alertThreshold = 0 }) => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [threshold, setThreshold] = useState(alertThreshold);
  const [tempThreshold, setTempThreshold] = useState(alertThreshold.toString());
  const [isAlertDisabled, setIsAlertDisabled] = useState(false);

  const formatValue = (val: number): string => {
    return Number.isInteger(val) ? val.toString() : val.toFixed(3);
  };

  const handleCardClick = () => {
    setIsDialogOpen(true);
  };

  const handleClose = () => {
    setIsDialogOpen(false);
    setTempThreshold(threshold.toString());
  };

  const handleSave = () => {
    setThreshold(Number(tempThreshold));
    setIsDialogOpen(false);
  };

  const handleThresholdChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setTempThreshold(event.target.value);
  };

  const handleToggleAlert = (event: React.MouseEvent) => {
    event.stopPropagation(); // Prevent triggering the card click event
    setIsAlertDisabled(prevState => !prevState);
  };

  const isAlert = threshold > 0 && value > threshold && !isAlertDisabled;

  return (
    <Paper elevation={3} style={{ backgroundColor: isAlert ? 'red' : 'white', color: isAlert ? 'white' : 'black' }}>
      <Box p={2} display="flex" justifyContent="space-between" alignItems="center" onClick={handleCardClick}>
        <Box>
          <Typography variant="h6" style={{ color: isAlert ? 'white' : 'black' }}>{title}</Typography>
          <Box display="flex" alignItems="baseline">
            <Typography variant="h4" style={{ color: isAlert ? 'white' : 'black' }}>
              {formatValue(value)}
            </Typography>
            {unit && (
              <Typography variant="subtitle2" style={{ marginLeft: 4, color: isAlert ? 'white' : 'black' }}>
                {unit}
              </Typography>
            )}
          </Box>
        </Box>
        <IconButton onClick={handleToggleAlert} style={{ color: isAlert ? 'white' : 'black' }}>
          {isAlertDisabled ? <NotificationsOffIcon /> : <NotificationsActiveIcon />}
        </IconButton>
      </Box>
      <Dialog open={isDialogOpen} onClose={handleClose}>
        <DialogTitle>Set Alert Threshold</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Please enter the new threshold value for {title}.
          </DialogContentText>
          <TextField
            autoFocus
            margin="dense"
            label="Threshold"
            type="number"
            fullWidth
            value={tempThreshold}
            onChange={handleThresholdChange}
            InputProps={{
              inputProps: {
                min: 0
              }
            }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>Cancel</Button>
          <Button onClick={handleSave}>Save</Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
};

export default MetricCard;
