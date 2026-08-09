USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'vehicles.PendingQuotaCompensations', N'U') IS NULL
BEGIN
    CREATE TABLE [vehicles].[PendingQuotaCompensations]
    (
        [Id]           BIGINT         IDENTITY(1,1) NOT NULL,
        [NodeKey]      VARCHAR(80)    NOT NULL,
        [OwnerType]    VARCHAR(20)    NOT NULL,
        [OwnerId]      BIGINT         NOT NULL,
        [Error]        NVARCHAR(400)  NULL,
        [CreatedAtUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_PendingQuotaCompensations_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_PendingQuotaCompensations] PRIMARY KEY CLUSTERED ([Id])
    );

    CREATE INDEX [IX_PendingQuotaCompensations_Owner] ON [vehicles].[PendingQuotaCompensations] ([OwnerType], [OwnerId], [NodeKey]);
END
GO
