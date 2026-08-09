USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'access.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE [access].[Roles]
    (
        [Id]          INT            IDENTITY(1,1) NOT NULL,
        [Code]        VARCHAR(40)    NOT NULL,
        [Name]        NVARCHAR(120)  NOT NULL,
        [IsActive]    BIT            NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3)  NOT NULL CONSTRAINT [DF_Roles_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

IF OBJECT_ID(N'access.Nodes', N'U') IS NULL
BEGIN
    CREATE TABLE [access].[Nodes]
    (
        [Key]         VARCHAR(80)   NOT NULL,
        [Label]       NVARCHAR(120) NOT NULL,
        [Icon]        NVARCHAR(2000) NOT NULL CONSTRAINT [DF_Nodes_Icon] DEFAULT (N''),
        [Route]       VARCHAR(200)  NOT NULL CONSTRAINT [DF_Nodes_Route] DEFAULT (''),
        [SortOrder]   INT           NOT NULL CONSTRAINT [DF_Nodes_SortOrder] DEFAULT (0),
        [IsActive]    BIT           NOT NULL CONSTRAINT [DF_Nodes_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Nodes_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Nodes] PRIMARY KEY CLUSTERED ([Key])
    );
END
GO

IF OBJECT_ID(N'access.RoleNodes', N'U') IS NULL
BEGIN
    CREATE TABLE [access].[RoleNodes]
    (
        [RoleId]  INT         NOT NULL,
        [NodeKey] VARCHAR(80)  NOT NULL,
        CONSTRAINT [PK_RoleNodes] PRIMARY KEY CLUSTERED ([RoleId], [NodeKey]),
        CONSTRAINT [FK_RoleNodes_Roles] FOREIGN KEY ([RoleId]) REFERENCES [access].[Roles] ([Id]) ON DELETE CASCADE
    );
END
GO
