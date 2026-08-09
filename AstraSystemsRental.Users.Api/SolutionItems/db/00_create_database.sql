IF DB_ID(N'AstraSystemsRental') IS NULL
BEGIN
    CREATE DATABASE [AstraSystemsRental];
END
GO

USE [AstraSystemsRental];
GO

IF SCHEMA_ID(N'users') IS NULL EXEC(N'CREATE SCHEMA [users]');
GO
IF SCHEMA_ID(N'subscriptions') IS NULL EXEC(N'CREATE SCHEMA [subscriptions]');
GO
IF SCHEMA_ID(N'access') IS NULL EXEC(N'CREATE SCHEMA [access]');
GO
