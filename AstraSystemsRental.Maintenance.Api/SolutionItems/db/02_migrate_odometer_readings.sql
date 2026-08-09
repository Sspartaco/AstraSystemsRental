USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'vehicles.FleetVehicleOdometerReadings', N'U') IS NOT NULL
   AND OBJECT_ID(N'maintenance.MileageReadings', N'U') IS NOT NULL
BEGIN
    INSERT INTO [maintenance].[MileageReadings]
        ([OwnerType], [OwnerId], [FleetVehicleId], [ReadingType], [ReadingDate], [Value], [Source], [Notes], [RecordedByUserId], [CreatedAtUtc])
    SELECT
        fv.[OwnerType],
        fv.[OwnerId],
        o.[FleetVehicleId],
        0,
        o.[ReadingDate],
        o.[Kilometers],
        'Import',
        o.[Notes],
        o.[RecordedByUserId],
        o.[CreatedAtUtc]
    FROM [vehicles].[FleetVehicleOdometerReadings] o
        INNER JOIN [vehicles].[FleetVehicles] fv ON fv.[Id] = o.[FleetVehicleId]
    WHERE NOT EXISTS (
        SELECT 1
        FROM [maintenance].[MileageReadings] m
        WHERE m.[FleetVehicleId] = o.[FleetVehicleId]
          AND m.[ReadingDate] = o.[ReadingDate]
          AND m.[Value] = o.[Kilometers]
    );
END
GO
