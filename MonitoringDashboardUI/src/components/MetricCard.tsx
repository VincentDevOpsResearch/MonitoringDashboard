import React, { useState } from 'react';
import { Paper, Typography, Box, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, TextField, IconButton, Button } from '@mui/material';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import NotificationsOffIcon from '@mui/icons-material/NotificationsOff';
import { fetchData } from '../api/requests';
import { UPDATE_THRESHOLDS } from '../api/constants';
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

interface MetricCardProps {
  title: string;
  value: number;
  unit?: string;
  alertThreshold?: number;
  alertMode?: number;
  isAlertEnabled?: boolean;
}

// Mapping between backend categories and display titles
// Mapping title to category (REVERSED)
const categoryTitleMap: Record<string, string> = {
  "Total Nodes": "TotalNodes",
  "Active Nodes": "ActiveNodes",
  "Total Pods": "TotalPods",
  "Running Pods": "RunningPods",
  "Requests Last 24 Hours": "RequestsLast24Hours",
  "Avg. Response Last 24 Hours": "AvgResponseLast24Hours",
  "Error Rate Last 24 Hours": "ErrorRateLast24Hours",
  "Requests This Hour": "RequestsThisHour",
  "Avg. Response This Hour": "AvgResponseThisHour",
  "Error Rate This Hour": "ErrorRateThisHour",
};

const MetricCard: React.FC<MetricCardProps> = ({ 
  title, 
  value, 
  unit = '', 
  alertThreshold = 0,
  alertMode = 0, 
  isAlertEnabled = false,
}) => {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [threshold, setThreshold] = useState(alertThreshold);
  const [tempThreshold, setTempThreshold] = useState(alertThreshold.toString());
  const [mode, setMode] = useState(alertMode);
  const [isAlertDisabled, setIsAlertDisabled] = useState(false);

  const formatValue = (val: number): string => {
    return Number.isInteger(val) ? val.toString() : val.toFixed(3);
  };

  const handleCardClick = () => {
    if (!isAlertEnabled) return; //Block clicking when alerts are d
    setIsDialogOpen(true);
  };

  const handleClose = () => {
    setIsDialogOpen(false);
    setTempThreshold(threshold.toString());
  };

  const handleSave = async () => {
    const newThreshold = Number(tempThreshold)
    const category = categoryTitleMap[title]

    try {
      await fetchData({
        path: UPDATE_THRESHOLDS,
        method: "POST",
        data: {
          category,
          threshold: newThreshold
        },
      });

      setThreshold(Number(tempThreshold));
      setIsDialogOpen(false);

      toast.success("Threshold updated successfully!", { position: "top-right", autoClose: 3000 });
    } catch (error) {
      toast.error("Failed to update threshold!", { position: "top-right", autoClose: 3000 });
    }
  };

  const handleThresholdChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setTempThreshold(event.target.value);
  };

  const handleToggleAlert = (event: React.MouseEvent) => {
    event.stopPropagation(); // Prevent triggering the card click event
    setIsAlertDisabled(prevState => !prevState);
  };

  const isAlert =
    isAlertEnabled &&
    threshold > 0 &&
    ((mode === 0 && value > threshold) || (mode === 1 && value !== threshold)) &&
    !isAlertDisabled;

    return (
      <Paper 
        elevation={3} 
        style={{ 
          backgroundColor: isAlert ? 'red' : 'white', 
          color: isAlert ? 'white' : 'black',
          cursor: isAlertEnabled ? 'pointer' : 'default', // Disable cursor if alerts are off
          opacity:  1 // Dim the card if alerts are off
        }}
      >
        <Box 
          p={2} 
          display="flex" 
          justifyContent="space-between" 
          alignItems="center" 
          onClick={handleCardClick}
        >
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
          {isAlertEnabled && ( // Hide alert button if alerts are disabled
            <IconButton onClick={handleToggleAlert} style={{ color: isAlert ? 'white' : 'black' }}>
              {isAlertDisabled ? <NotificationsOffIcon /> : <NotificationsActiveIcon />}
            </IconButton>
          )}
        </Box>
        {isAlertEnabled && ( // Hide dialog if alerts are disabled
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
        )}
      </Paper>
    );
  };

export default MetricCard;
