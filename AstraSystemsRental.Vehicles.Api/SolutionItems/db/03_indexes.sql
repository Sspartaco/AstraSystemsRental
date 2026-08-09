USE [AstraSystemsRental];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ValuationCache_Plate_Source' AND object_id = OBJECT_ID(N'vehicles.ValuationCache'))
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ValuationCache_Plate_Source]
        ON [vehicles].[ValuationCache] ([PlateNumber], [SourceCode]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_QuoteRequests_RequestId' AND object_id = OBJECT_ID(N'vehicles.QuoteRequests'))
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_QuoteRequests_RequestId]
        ON [vehicles].[QuoteRequests] ([RequestId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_QuoteRequests_Status_CompletedAtUtc' AND object_id = OBJECT_ID(N'vehicles.QuoteRequests'))
    CREATE NONCLUSTERED INDEX [IX_QuoteRequests_Status_CompletedAtUtc]
        ON [vehicles].[QuoteRequests] ([Status], [CompletedAtUtc]);
GO
