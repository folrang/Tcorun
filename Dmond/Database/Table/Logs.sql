
-- AppLogDb & dbo.Logs
IF DB_ID('AppLogDb') IS NULL CREATE DATABASE AppLogDb;
GO
USE AppLogDb;
GO
IF OBJECT_ID('dbo.Logs') IS NULL
BEGIN
  CREATE TABLE dbo.Logs
  (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Level VARCHAR(16)      NOT NULL,
    Source NVARCHAR(128)   NULL,
    Message NVARCHAR(1024) NOT NULL,
    JsonData NVARCHAR(MAX) NULL,
    RequestId UNIQUEIDENTIFIER NULL,
    ErrorCode INT          NULL,
    ClientIp VARCHAR(64)   NULL,
    Tags NVARCHAR(256)     NULL,
    CreatedUtc DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME()
  );
  CREATE INDEX IX_Logs_Level_CreatedUtc ON dbo.Logs(Level, CreatedUtc DESC);
  CREATE INDEX IX_Logs_RequestId       ON dbo.Logs(RequestId);
END
GO

