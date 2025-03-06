USE [master];
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MonitoringDB')
BEGIN
    CREATE DATABASE MonitoringDB;
END
GO

-- Switch to the created database
USE [MonitoringDB];
GO

-- Create the NodeMetrics table
CREATE TABLE NodeMetrics (
    Id INT PRIMARY KEY IDENTITY,  -- Auto-incrementing primary key
    NodeName NVARCHAR(255) NOT NULL,  -- Node name
    CpuUsage FLOAT NOT NULL,  -- CPU usage percentage
    MemoryUsage FLOAT NOT NULL,  -- Memory usage percentage
    DiskUsage FLOAT NOT NULL,  -- Disk usage percentage
    Timestamp DATETIME DEFAULT GETDATE()  -- Data collection timestamp, default to current time
);
GO

-- Create the ClusterMetrics table
CREATE TABLE ClusterMetrics (
    Id INT PRIMARY KEY IDENTITY,  -- Auto-incrementing primary key
    TotalNodes INT NOT NULL,  -- Total number of nodes
    ActiveNodes INT NOT NULL,  -- Number of active nodes
    CpuUsage FLOAT NOT NULL,  -- Cluster CPU usage percentage
    MemoryUsage FLOAT NOT NULL,  -- Cluster memory usage percentage
    DiskUsage FLOAT,  -- Cluster disk usage percentage (nullable)
    Timestamp DATETIME DEFAULT GETDATE()  -- Data collection timestamp, default to current time
);
GO

-- Create the PodMetrics table
CREATE TABLE PodMetrics (
    Id INT PRIMARY KEY IDENTITY,  -- Auto-incrementing primary key
    TotalPods INT NOT NULL,  -- Total number of pods
    RunningPods INT NOT NULL,  -- Number of running pods
    Timestamp DATETIME DEFAULT GETDATE()  -- Data collection timestamp, default to current time
);
GO