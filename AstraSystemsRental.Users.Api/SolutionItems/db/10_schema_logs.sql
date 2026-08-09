IF SCHEMA_ID('logs') IS NULL
    EXEC('CREATE SCHEMA logs');
GO

IF OBJECT_ID('logs.ApplicationLogs', 'U') IS NULL
BEGIN
    CREATE TABLE logs.ApplicationLogs
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        TimestampUtc    DATETIME2(3)   NOT NULL CONSTRAINT DF_ApplicationLogs_TimestampUtc DEFAULT SYSUTCDATETIME(),
        Level           VARCHAR(16)    NOT NULL,
        Service         VARCHAR(64)    NOT NULL,
        Message         NVARCHAR(2000) NOT NULL,
        ExceptionType   VARCHAR(256)   NULL,
        ExceptionDetail NVARCHAR(MAX)  NULL,
        TraceId         VARCHAR(64)    NULL,
        RequestMethod   VARCHAR(10)    NULL,
        RequestPath     NVARCHAR(512)  NULL,
        StatusCode      INT            NULL,
        UserId          BIGINT         NULL,
        UserEmail       NVARCHAR(256)  NULL,
        CONSTRAINT PK_ApplicationLogs PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationLogs_TimestampUtc' AND object_id = OBJECT_ID('logs.ApplicationLogs'))
    CREATE INDEX IX_ApplicationLogs_TimestampUtc ON logs.ApplicationLogs (TimestampUtc DESC) INCLUDE (Level, Service);
GO

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationLogs_TraceId' AND object_id = OBJECT_ID('logs.ApplicationLogs'))
    CREATE INDEX IX_ApplicationLogs_TraceId ON logs.ApplicationLogs (TraceId) WHERE TraceId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationLogs_Level' AND object_id = OBJECT_ID('logs.ApplicationLogs'))
    CREATE INDEX IX_ApplicationLogs_Level ON logs.ApplicationLogs (Level, TimestampUtc DESC);
GO

IF NOT EXISTS (SELECT 1 FROM access.Roles WHERE Code = 'SysAdmin')
    INSERT INTO access.Roles (Code, Name) VALUES ('SysAdmin', 'Administrador de sistemas');
GO

DECLARE @SysAdminId INT = (SELECT Id FROM access.Roles WHERE Code = 'SysAdmin');

IF @SysAdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM access.RoleNodes WHERE RoleId = @SysAdminId AND NodeKey = '*')
    INSERT INTO access.RoleNodes (RoleId, NodeKey) VALUES (@SysAdminId, '*');
GO
