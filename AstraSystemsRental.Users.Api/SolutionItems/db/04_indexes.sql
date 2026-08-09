USE [AstraSystemsRental];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_Owner')
    CREATE INDEX [IX_Subscriptions_Owner] ON [subscriptions].[Subscriptions] ([OwnerType], [OwnerId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyMembers_UserId')
    CREATE INDEX [IX_CompanyMembers_UserId] ON [companies].[CompanyMembers] ([UserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Companies_DocumentNumber')
    CREATE UNIQUE INDEX [UQ_Companies_DocumentNumber] ON [companies].[Companies] ([DocumentNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Users_Email')
    CREATE UNIQUE INDEX [UQ_Users_Email] ON [users].[Users] ([Email]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Persons_DocumentNumber')
    CREATE INDEX [IX_Persons_DocumentNumber] ON [users].[Persons] ([DocumentNumber]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_EmailConfirmations_Token')
    CREATE UNIQUE INDEX [UQ_EmailConfirmations_Token] ON [users].[EmailConfirmations] ([Token]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailConfirmations_ExpiresAtUtc')
    CREATE INDEX [IX_EmailConfirmations_ExpiresAtUtc] ON [users].[EmailConfirmations] ([ExpiresAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailConfirmations_UserId')
    CREATE INDEX [IX_EmailConfirmations_UserId] ON [users].[EmailConfirmations] ([UserId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Plans_Code')
    CREATE UNIQUE INDEX [UQ_Plans_Code] ON [subscriptions].[Plans] ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Roles_Code')
    CREATE UNIQUE INDEX [UQ_Roles_Code] ON [access].[Roles] ([Code]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_EndsAtUtc')
    CREATE INDEX [IX_Subscriptions_EndsAtUtc] ON [subscriptions].[Subscriptions] ([EndsAtUtc]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_PersonId')
    CREATE INDEX [IX_Users_PersonId] ON [users].[Users] ([PersonId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_RoleId')
    CREATE INDEX [IX_Users_RoleId] ON [users].[Users] ([RoleId]);
GO
