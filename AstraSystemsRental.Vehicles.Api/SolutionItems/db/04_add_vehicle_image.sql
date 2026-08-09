USE [AstraSystemsRental];
GO

IF COL_LENGTH(N'vehicles.VehicleCatalog', N'ImageUrl') IS NULL
BEGIN
    ALTER TABLE [vehicles].[VehicleCatalog] ADD [ImageUrl] NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'vehicles.VehicleCatalog', N'ImageAttribution') IS NULL
BEGIN
    ALTER TABLE [vehicles].[VehicleCatalog] ADD [ImageAttribution] NVARCHAR(300) NULL;
END
GO

IF COL_LENGTH(N'vehicles.VehicleCatalog', N'ImageFetchedAtUtc') IS NULL
BEGIN
    ALTER TABLE [vehicles].[VehicleCatalog] ADD [ImageFetchedAtUtc] DATETIME2(3) NULL;
END
GO
