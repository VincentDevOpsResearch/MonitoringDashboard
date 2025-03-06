import React, { useState } from "react";
import InfrastructurePerformance from "./InfrasctructurePeformance";
import ClusterPerformance from "./ClusterPerformance";
import "./Dashboard.css";

const Dashboard: React.FC = () => {

  return (
    <div className="dashboard">
      <div className="left-section">
        <InfrastructurePerformance />
      </div>
      <div className="right-section">
        <ClusterPerformance />
      </div>
    </div>
  );
};

export default Dashboard;
