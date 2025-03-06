import React from "react";
import ComputePerformance from "./ComputePerformance";
import { Typography } from "@mui/material";

const InfrastructurePerformance: React.FC = () => {
  return (
    <div className="infrastructure-performance">
      <Typography variant="h4" align="center" gutterBottom>Overall Infrastructure Performance</Typography>
      <ComputePerformance />
    </div>
  );
};

export default InfrastructurePerformance;
