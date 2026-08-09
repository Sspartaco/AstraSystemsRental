USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'vehicles.FleetVehicles', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[FleetVehicles]
    (
        [Id]                    BIGINT         IDENTITY(1,1) NOT NULL,
        [OwnerType]             VARCHAR(20)    NOT NULL,
        [OwnerId]               BIGINT         NOT NULL,
        [PlateNumber]           VARCHAR(10)    NOT NULL,
        [VehicleClass]          NVARCHAR(60)   NULL,
        [Brand]                 NVARCHAR(80)   NULL,
        [Line]                  NVARCHAR(120)  NULL,
        [ModelYear]             SMALLINT       NULL,
        [BodyType]              NVARCHAR(60)   NULL,
        [ServiceType]           NVARCHAR(40)   NULL,
        [FuelType]              NVARCHAR(40)   NULL,
        [Transmission]          NVARCHAR(40)   NULL,
        [Color]                 NVARCHAR(40)   NULL,
        [Vin]                   VARCHAR(32)    NULL,
        [EngineNumber]          VARCHAR(32)    NULL,
        [SerialNumber]          VARCHAR(32)    NULL,
        [ChassisNumber]         VARCHAR(32)    NULL,
        [TransitLicenseNumber]  VARCHAR(40)    NULL,
        [PurchaseInvoiceNumber] VARCHAR(40)    NULL,
        [PurchaseDate]          DATE           NULL,
        [PurchaseValue]         DECIMAL(14,2)  NULL,
        [Status]                TINYINT        NOT NULL CONSTRAINT [DF_FleetVehicles_Status] DEFAULT (0),
        [Notes]                 NVARCHAR(1000) NULL,
        [RowVersion]            ROWVERSION     NOT NULL,
        [CreatedByUserId]       BIGINT         NOT NULL,
        [CreatedAtUtc]          DATETIME2(3)   NOT NULL CONSTRAINT [DF_FleetVehicles_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc]          DATETIME2(3)   NOT NULL CONSTRAINT [DF_FleetVehicles_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_FleetVehicles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_FleetVehicles_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE UNIQUE INDEX [UX_FleetVehicles_Owner_Plate] ON [vehicles].[FleetVehicles] ([OwnerType], [OwnerId], [PlateNumber]);
    CREATE INDEX [IX_FleetVehicles_Owner_Status] ON [vehicles].[FleetVehicles] ([OwnerType], [OwnerId], [Status]);
END
GO

IF OBJECT_ID(N'vehicles.FleetVehicleStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[FleetVehicleStatusHistory]
    (
        [Id]              BIGINT       IDENTITY(1,1) NOT NULL,
        [FleetVehicleId]  BIGINT       NOT NULL,
        [PreviousStatus]  TINYINT      NULL,
        [NewStatus]       TINYINT      NOT NULL,
        [Reason]          NVARCHAR(400) NULL,
        [ChangedByUserId] BIGINT       NOT NULL,
        [ChangedAtUtc]    DATETIME2(3) NOT NULL CONSTRAINT [DF_FleetVehicleStatusHistory_ChangedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_FleetVehicleStatusHistory] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FleetVehicleStatusHistory_FleetVehicles] FOREIGN KEY ([FleetVehicleId]) REFERENCES [vehicles].[FleetVehicles] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_FleetVehicleStatusHistory_Vehicle_Date] ON [vehicles].[FleetVehicleStatusHistory] ([FleetVehicleId], [ChangedAtUtc] DESC);
END
GO

IF OBJECT_ID(N'vehicles.FleetVehicleOdometerReadings', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[FleetVehicleOdometerReadings]
    (
        [Id]                BIGINT       IDENTITY(1,1) NOT NULL,
        [FleetVehicleId]    BIGINT       NOT NULL,
        [Kilometers]        INT          NOT NULL,
        [ReadingDate]       DATE         NOT NULL,
        [Source]            VARCHAR(20)  NOT NULL CONSTRAINT [DF_FleetVehicleOdometerReadings_Source] DEFAULT ('Manual'),
        [Notes]             NVARCHAR(400) NULL,
        [RecordedByUserId]  BIGINT       NOT NULL,
        [CreatedAtUtc]      DATETIME2(3) NOT NULL CONSTRAINT [DF_FleetVehicleOdometerReadings_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_FleetVehicleOdometerReadings] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FleetVehicleOdometerReadings_FleetVehicles] FOREIGN KEY ([FleetVehicleId]) REFERENCES [vehicles].[FleetVehicles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_FleetVehicleOdometerReadings_Kilometers] CHECK ([Kilometers] >= 0)
    );

    CREATE INDEX [IX_FleetVehicleOdometerReadings_Vehicle_Date] ON [vehicles].[FleetVehicleOdometerReadings] ([FleetVehicleId], [ReadingDate] DESC);
END
GO

IF OBJECT_ID(N'vehicles.FleetVehicleDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[FleetVehicleDocuments]
    (
        [Id]              BIGINT        IDENTITY(1,1) NOT NULL,
        [FleetVehicleId]  BIGINT        NOT NULL,
        [DocumentType]    VARCHAR(40)   NOT NULL,
        [DocumentNumber]  VARCHAR(60)   NULL,
        [IssuedDate]      DATE          NULL,
        [ExpiresDate]     DATE          NULL,
        [Status]          TINYINT       NOT NULL CONSTRAINT [DF_FleetVehicleDocuments_Status] DEFAULT (2),
        [Notes]           NVARCHAR(400) NULL,
        [CreatedByUserId] BIGINT        NOT NULL,
        [CreatedAtUtc]    DATETIME2(3)  NOT NULL CONSTRAINT [DF_FleetVehicleDocuments_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc]    DATETIME2(3)  NOT NULL CONSTRAINT [DF_FleetVehicleDocuments_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_FleetVehicleDocuments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FleetVehicleDocuments_FleetVehicles] FOREIGN KEY ([FleetVehicleId]) REFERENCES [vehicles].[FleetVehicles] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_FleetVehicleDocuments_Vehicle] ON [vehicles].[FleetVehicleDocuments] ([FleetVehicleId]);
    CREATE INDEX [IX_FleetVehicleDocuments_ExpiresDate] ON [vehicles].[FleetVehicleDocuments] ([ExpiresDate]);
END
GO
