USE [master];
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MonitoringDB')
BEGIN
    CREATE DATABASE MonitoringDB;
END
GO

USE [MonitoringDB];
GO


CREATE TABLE CpuForecasts (
    id INT IDENTITY(1,1) PRIMARY KEY,
    item_id NVARCHAR(50) NOT NULL,
    timestamp DATETIME NOT NULL,
    mean FLOAT NOT NULL,
    percentile_10 FLOAT NOT NULL,
    percentile_90 FLOAT NOT NULL,
    created_at DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE MemoryForecasts (
    id INT IDENTITY(1,1) PRIMARY KEY,
    item_id NVARCHAR(50) NOT NULL,
    timestamp DATETIME NOT NULL,
    mean FLOAT NOT NULL,
    percentile_10 FLOAT NOT NULL,
    percentile_90 FLOAT NOT NULL,
    created_at DATETIME DEFAULT GETDATE()
);
GO