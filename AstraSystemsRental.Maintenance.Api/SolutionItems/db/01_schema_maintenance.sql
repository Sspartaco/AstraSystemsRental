USE [AstraSystemsRental];
GO

IF SCHEMA_ID(N'maintenance') IS NULL
    EXEC(N'CREATE SCHEMA [maintenance]');
GO

IF OBJECT_ID(N'maintenance.MaintenanceRoutines', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[MaintenanceRoutines]
    (
        [Id]              BIGINT         IDENTITY(1,1) NOT NULL,
        [OwnerType]       VARCHAR(20)    NOT NULL,
        [OwnerId]         BIGINT         NOT NULL,
        [Name]            NVARCHAR(150)  NOT NULL,
        [Description]     NVARCHAR(400)  NULL,
        [IsActive]        BIT            NOT NULL CONSTRAINT [DF_MaintenanceRoutines_IsActive] DEFAULT (1),
        [RowVersion]      ROWVERSION     NOT NULL,
        [CreatedByUserId] BIGINT         NOT NULL,
        [CreatedAtUtc]    DATETIME2(3)   NOT NULL CONSTRAINT [DF_MaintenanceRoutines_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc]    DATETIME2(3)   NOT NULL CONSTRAINT [DF_MaintenanceRoutines_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_MaintenanceRoutines] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_MaintenanceRoutines_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE UNIQUE INDEX [UX_MaintenanceRoutines_Owner_Name] ON [maintenance].[MaintenanceRoutines] ([OwnerType], [OwnerId], [Name]);
END
GO

IF OBJECT_ID(N'maintenance.MaintenanceRoutinePeriodicities', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[MaintenanceRoutinePeriodicities]
    (
        [Id]           BIGINT   IDENTITY(1,1) NOT NULL,
        [RoutineId]    BIGINT   NOT NULL,
        [Unit]         TINYINT  NOT NULL,
        [StartsAt]     INT      NOT NULL CONSTRAINT [DF_MaintenanceRoutinePeriodicities_StartsAt] DEFAULT (0),
        [RepeatsEvery] INT      NOT NULL,
        CONSTRAINT [PK_MaintenanceRoutinePeriodicities] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MaintenanceRoutinePeriodicities_Routines] FOREIGN KEY ([RoutineId]) REFERENCES [maintenance].[MaintenanceRoutines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_MaintenanceRoutinePeriodicities_RepeatsEvery] CHECK ([RepeatsEvery] > 0)
    );

    CREATE INDEX [IX_MaintenanceRoutinePeriodicities_Routine] ON [maintenance].[MaintenanceRoutinePeriodicities] ([RoutineId]);
END
GO

IF OBJECT_ID(N'maintenance.MaintenanceRoutineConcepts', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[MaintenanceRoutineConcepts]
    (
        [Id]            BIGINT         IDENTITY(1,1) NOT NULL,
        [PeriodicityId] BIGINT         NOT NULL,
        [Name]          NVARCHAR(150)  NOT NULL,
        [Quantity]      DECIMAL(14,2)  NOT NULL CONSTRAINT [DF_MaintenanceRoutineConcepts_Quantity] DEFAULT (1),
        [QuantityUnit]  TINYINT        NULL,
        [Notes]         NVARCHAR(400)  NULL,
        CONSTRAINT [PK_MaintenanceRoutineConcepts] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MaintenanceRoutineConcepts_Periodicities] FOREIGN KEY ([PeriodicityId]) REFERENCES [maintenance].[MaintenanceRoutinePeriodicities] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_MaintenanceRoutineConcepts_Periodicity] ON [maintenance].[MaintenanceRoutineConcepts] ([PeriodicityId]);
END
GO

IF OBJECT_ID(N'maintenance.RoutineAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[RoutineAssignments]
    (
        [Id]               BIGINT       IDENTITY(1,1) NOT NULL,
        [OwnerType]        VARCHAR(20)  NOT NULL,
        [OwnerId]          BIGINT       NOT NULL,
        [FleetVehicleId]   BIGINT       NOT NULL,
        [RoutineId]        BIGINT       NOT NULL,
        [AssignedAtUtc]    DATETIME2(3) NOT NULL CONSTRAINT [DF_RoutineAssignments_AssignedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [AssignedByUserId] BIGINT       NOT NULL,
        [RowVersion]       ROWVERSION   NOT NULL,
        CONSTRAINT [PK_RoutineAssignments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_RoutineAssignments_Routines] FOREIGN KEY ([RoutineId]) REFERENCES [maintenance].[MaintenanceRoutines] ([Id]),
        CONSTRAINT [CK_RoutineAssignments_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE UNIQUE INDEX [UX_RoutineAssignments_Owner_Vehicle] ON [maintenance].[RoutineAssignments] ([OwnerType], [OwnerId], [FleetVehicleId]);
END
GO

IF OBJECT_ID(N'maintenance.RoutineAssignmentHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[RoutineAssignmentHistory]
    (
        [Id]                BIGINT       IDENTITY(1,1) NOT NULL,
        [FleetVehicleId]    BIGINT       NOT NULL,
        [PreviousRoutineId] BIGINT       NULL,
        [NewRoutineId]      BIGINT       NOT NULL,
        [ChangedByUserId]   BIGINT       NOT NULL,
        [ChangedAtUtc]      DATETIME2(3) NOT NULL CONSTRAINT [DF_RoutineAssignmentHistory_ChangedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_RoutineAssignmentHistory] PRIMARY KEY CLUSTERED ([Id])
    );

    CREATE INDEX [IX_RoutineAssignmentHistory_Vehicle_Date] ON [maintenance].[RoutineAssignmentHistory] ([FleetVehicleId], [ChangedAtUtc] DESC);
END
GO

IF OBJECT_ID(N'maintenance.MileageReadings', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[MileageReadings]
    (
        [Id]                  BIGINT        IDENTITY(1,1) NOT NULL,
        [OwnerType]           VARCHAR(20)   NOT NULL,
        [OwnerId]             BIGINT        NOT NULL,
        [FleetVehicleId]      BIGINT        NOT NULL,
        [ReadingType]         TINYINT       NOT NULL CONSTRAINT [DF_MileageReadings_ReadingType] DEFAULT (0),
        [ReadingDate]         DATE          NOT NULL,
        [Value]               INT           NOT NULL,
        [Source]              VARCHAR(20)   NOT NULL CONSTRAINT [DF_MileageReadings_Source] DEFAULT ('Manual'),
        [SourceReservationId] BIGINT        NULL,
        [Notes]               NVARCHAR(400) NULL,
        [RecordedByUserId]    BIGINT        NOT NULL,
        [CreatedAtUtc]        DATETIME2(3)  NOT NULL CONSTRAINT [DF_MileageReadings_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_MileageReadings] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_MileageReadings_Value] CHECK ([Value] >= 0),
        CONSTRAINT [CK_MileageReadings_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE INDEX [IX_MileageReadings_Owner_Vehicle_Date] ON [maintenance].[MileageReadings] ([OwnerType], [OwnerId], [FleetVehicleId], [ReadingDate] DESC);
END
GO

IF OBJECT_ID(N'maintenance.WorkshopProviders', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[WorkshopProviders]
    (
        [Id]             BIGINT         IDENTITY(1,1) NOT NULL,
        [OwnerType]      VARCHAR(20)    NOT NULL,
        [OwnerId]        BIGINT         NOT NULL,
        [ProviderType]   TINYINT        NOT NULL CONSTRAINT [DF_WorkshopProviders_ProviderType] DEFAULT (1),
        [Name]           NVARCHAR(160)  NOT NULL,
        [DocumentNumber] VARCHAR(40)    NULL,
        [ContactPhone]   VARCHAR(40)    NULL,
        [ContactEmail]   NVARCHAR(256)  NULL,
        [Address]        NVARCHAR(250)  NULL,
        [IsActive]       BIT            NOT NULL CONSTRAINT [DF_WorkshopProviders_IsActive] DEFAULT (1),
        [CreatedAtUtc]   DATETIME2(3)   NOT NULL CONSTRAINT [DF_WorkshopProviders_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc]   DATETIME2(3)   NOT NULL CONSTRAINT [DF_WorkshopProviders_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_WorkshopProviders] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_WorkshopProviders_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE INDEX [IX_WorkshopProviders_Owner_Name] ON [maintenance].[WorkshopProviders] ([OwnerType], [OwnerId], [Name]);
END
GO

IF OBJECT_ID(N'maintenance.WorkshopReservations', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[WorkshopReservations]
    (
        [Id]                   BIGINT         IDENTITY(1,1) NOT NULL,
        [OwnerType]            VARCHAR(20)    NOT NULL,
        [OwnerId]              BIGINT         NOT NULL,
        [FleetVehicleId]       BIGINT         NOT NULL,
        [ProviderId]           BIGINT         NULL,
        [Status]               TINYINT        NOT NULL CONSTRAINT [DF_WorkshopReservations_Status] DEFAULT (0),
        [ScheduledAtUtc]       DATETIME2(3)   NOT NULL,
        [ExpectedEndAtUtc]     DATETIME2(3)   NULL,
        [PickedUpAtUtc]        DATETIME2(3)   NULL,
        [ReadyAtUtc]           DATETIME2(3)   NULL,
        [CollectedAtUtc]       DATETIME2(3)   NULL,
        [MileageAtReservation] INT            NULL,
        [IsWashOnly]           BIT            NOT NULL CONSTRAINT [DF_WorkshopReservations_IsWashOnly] DEFAULT (0),
        [Notes]                NVARCHAR(1000) NULL,
        [ReservedByUserId]     BIGINT         NOT NULL,
        [RowVersion]           ROWVERSION     NOT NULL,
        [CreatedAtUtc]         DATETIME2(3)   NOT NULL CONSTRAINT [DF_WorkshopReservations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        [UpdatedAtUtc]         DATETIME2(3)   NOT NULL CONSTRAINT [DF_WorkshopReservations_UpdatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_WorkshopReservations] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_WorkshopReservations_Providers] FOREIGN KEY ([ProviderId]) REFERENCES [maintenance].[WorkshopProviders] ([Id]),
        CONSTRAINT [CK_WorkshopReservations_OwnerType] CHECK ([OwnerType] IN ('User', 'Company'))
    );

    CREATE INDEX [IX_WorkshopReservations_Owner_Vehicle_Status] ON [maintenance].[WorkshopReservations] ([OwnerType], [OwnerId], [FleetVehicleId], [Status]);
END
GO

IF OBJECT_ID(N'maintenance.WorkshopReservationPhotos', N'U') IS NULL
BEGIN
    CREATE TABLE [maintenance].[WorkshopReservationPhotos]
    (
        [Id]                    BIGINT         IDENTITY(1,1) NOT NULL,
        [WorkshopReservationId] BIGINT         NOT NULL,
        [Status]                TINYINT        NOT NULL,
        [FileName]              NVARCHAR(260)  NOT NULL,
        [StoragePath]           NVARCHAR(400)  NOT NULL,
        [ContentType]           VARCHAR(100)   NULL,
        [SizeBytes]             BIGINT         NOT NULL CONSTRAINT [DF_WorkshopReservationPhotos_SizeBytes] DEFAULT (0),
        [Notes]                 NVARCHAR(400)  NULL,
        [UploadedByUserId]      BIGINT         NOT NULL,
        [CreatedAtUtc]          DATETIME2(3)   NOT NULL CONSTRAINT [DF_WorkshopReservationPhotos_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_WorkshopReservationPhotos] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_WorkshopReservationPhotos_Reservations] FOREIGN KEY ([WorkshopReservationId]) REFERENCES [maintenance].[WorkshopReservations] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WorkshopReservationPhotos_Reservation] ON [maintenance].[WorkshopReservationPhotos] ([WorkshopReservationId]);
END
GO
