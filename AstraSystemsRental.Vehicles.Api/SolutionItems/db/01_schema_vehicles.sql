USE [AstraSystemsRental];
GO

IF SCHEMA_ID(N'vehicles') IS NULL EXEC(N'CREATE SCHEMA [vehicles]');
GO

IF OBJECT_ID(N'vehicles.VehicleCatalog', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[VehicleCatalog]
    (
        [PlateNumber]  VARCHAR(10)   NOT NULL,
        [VehicleClass] NVARCHAR(60)  NULL,
        [Brand]        NVARCHAR(80)  NULL,
        [Line]         NVARCHAR(120) NULL,
        [ModelYear]    SMALLINT      NULL,
        [FullLine]     NVARCHAR(200) NULL,
        [Engine]       NVARCHAR(60)  NULL,
        [IsUserAdded]  BIT           NOT NULL CONSTRAINT [DF_VehicleCatalog_IsUserAdded] DEFAULT (0),
        [CreatedAtUtc] DATETIME2(3)  NOT NULL CONSTRAINT [DF_VehicleCatalog_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc] DATETIME2(3)  NOT NULL CONSTRAINT [DF_VehicleCatalog_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_VehicleCatalog] PRIMARY KEY CLUSTERED ([PlateNumber])
    );
END
GO

IF OBJECT_ID(N'vehicles.ValuationSources', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[ValuationSources]
    (
        [Code]        VARCHAR(30)  NOT NULL,
        [DisplayName] NVARCHAR(60) NOT NULL,
        [IsActive]    BIT          NOT NULL CONSTRAINT [DF_ValuationSources_IsActive] DEFAULT (1),
        [SortOrder]   INT          NOT NULL CONSTRAINT [DF_ValuationSources_SortOrder] DEFAULT (0),
        CONSTRAINT [PK_ValuationSources] PRIMARY KEY CLUSTERED ([Code])
    );
END
GO

IF OBJECT_ID(N'vehicles.ValuationCache', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[ValuationCache]
    (
        [Id]           BIGINT         IDENTITY(1,1) NOT NULL,
        [PlateNumber]  VARCHAR(10)    NOT NULL,
        [SourceCode]   VARCHAR(30)    NOT NULL,
        [Status]       TINYINT        NOT NULL CONSTRAINT [DF_ValuationCache_Status] DEFAULT (0),
        [ValueMin]     DECIMAL(14,2)  NULL,
        [ValueMax]     DECIMAL(14,2)  NULL,
        [ValueAvg]     DECIMAL(14,2)  NULL,
        [Currency]     CHAR(3)        NOT NULL CONSTRAINT [DF_ValuationCache_Currency] DEFAULT ('COP'),
        [RawPayload]   NVARCHAR(MAX)  NULL,
        [FetchedAtUtc] DATETIME2(3)   NULL,
        [ExpiresAtUtc] DATETIME2(3)   NULL,
        [ErrorMessage] NVARCHAR(400)  NULL,
        CONSTRAINT [PK_ValuationCache] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ValuationCache_ValuationSources] FOREIGN KEY ([SourceCode]) REFERENCES [vehicles].[ValuationSources] ([Code])
    );
END
GO

IF OBJECT_ID(N'vehicles.QuoteRequests', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[QuoteRequests]
    (
        [Id]                BIGINT           IDENTITY(1,1) NOT NULL,
        [RequestId]         UNIQUEIDENTIFIER NOT NULL,
        [PlateNumber]       VARCHAR(10)      NOT NULL,
        [RequestedByUserId] BIGINT           NOT NULL,
        [Status]            TINYINT          NOT NULL CONSTRAINT [DF_QuoteRequests_Status] DEFAULT (0),
        [CreatedAtUtc]      DATETIME2(3)     NOT NULL CONSTRAINT [DF_QuoteRequests_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [CompletedAtUtc]    DATETIME2(3)     NULL,
        CONSTRAINT [PK_QuoteRequests] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO
