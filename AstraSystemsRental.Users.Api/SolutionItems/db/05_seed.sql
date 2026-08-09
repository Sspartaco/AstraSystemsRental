USE [AstraSystemsRental];
GO

MERGE [access].[Roles] AS target
USING (VALUES
    ('Standard',  N'Standard user'),
    ('Demo',      N'Demo user'),
    ('SuperUser', N'Super user')
) AS source ([Code], [Name])
ON target.[Code] = source.[Code]
WHEN NOT MATCHED THEN
    INSERT ([Code], [Name]) VALUES (source.[Code], source.[Name]);
GO

MERGE [access].[Nodes] AS target
USING (VALUES
    ('dashboard',        N'Panel',      N'<rect x="3" y="3" width="7.5" height="7.5" rx="1.5" /><rect x="13.5" y="3" width="7.5" height="7.5" rx="1.5" /><rect x="3" y="13.5" width="7.5" height="7.5" rx="1.5" /><rect x="13.5" y="13.5" width="7.5" height="7.5" rx="1.5" />', 'Home/Index',            10),
    ('fleet',            N'Vehículos',  N'<path d="M2.75 6.75A1.75 1.75 0 0 1 4.5 5h8.75a1.75 1.75 0 0 1 1.75 1.75V15H3.5a.75.75 0 0 1-.75-.75V6.75Z" /><path d="M15 8.5h2.94a1.75 1.75 0 0 1 1.55.94l1.56 3a1.75 1.75 0 0 1 .2.81V15H15V8.5Z" /><circle cx="7" cy="17" r="1.75" /><circle cx="17.5" cy="17" r="1.75" /><path d="M8.75 17H15" />', 'Fleet/Index',           20),
    ('reports',          N'Reportes',   N'<path d="M4.5 19.5V9m5.25 10.5V4.5m5.25 15v-7.5m5.25 7.5V7.5" />', 'Reports/Index',        30),
    ('vehicle-registry', N'Mi Flota',   N'<rect x="3" y="7.5" width="18" height="12" rx="1.5" /><path d="M7.5 7.5V6a1.5 1.5 0 0 1 1.5-1.5h6A1.5 1.5 0 0 1 16.5 6v1.5" /><path d="M3 12.75h18" />', 'VehicleRegistry/Index', 25)
) AS source ([Key], [Label], [Icon], [Route], [SortOrder])
ON target.[Key] = source.[Key]
WHEN NOT MATCHED THEN
    INSERT ([Key], [Label], [Icon], [Route], [SortOrder])
    VALUES (source.[Key], source.[Label], source.[Icon], source.[Route], source.[SortOrder]);
GO

UPDATE [access].[Nodes] SET [Label] = N'Vehículos' WHERE [Key] = 'fleet' AND [Label] <> N'Vehículos';
GO

MERGE [subscriptions].[Plans] AS target
USING (VALUES
    ('Demo',  N'Demo plan',      3),
    ('Basic', N'Standard plan', 30),
    ('Full',  N'Full plan',     30)
) AS source ([Code], [Name], [DurationDays])
ON target.[Code] = source.[Code]
WHEN NOT MATCHED THEN
    INSERT ([Code], [Name], [DurationDays]) VALUES (source.[Code], source.[Name], source.[DurationDays])
WHEN MATCHED AND target.[Name] <> source.[Name] THEN
    UPDATE SET [Name] = source.[Name];
GO

DECLARE @demoPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Demo');
DECLARE @basicPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Basic');
DECLARE @fullPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Full');

MERGE [subscriptions].[PlanNodes] AS target
USING (VALUES
    (@demoPlanId,  'dashboard'),
    (@demoPlanId,  'fleet'),
    (@demoPlanId,  'vehicle-registry'),
    (@basicPlanId, 'dashboard'),
    (@basicPlanId, 'fleet'),
    (@basicPlanId, 'reports'),
    (@basicPlanId, 'vehicle-registry'),
    (@fullPlanId,  'dashboard'),
    (@fullPlanId,  'fleet'),
    (@fullPlanId,  'reports'),
    (@fullPlanId,  'vehicle-registry')
) AS source ([PlanId], [NodeKey])
ON target.[PlanId] = source.[PlanId] AND target.[NodeKey] = source.[NodeKey]
WHEN NOT MATCHED THEN
    INSERT ([PlanId], [NodeKey]) VALUES (source.[PlanId], source.[NodeKey]);
GO

MERGE [subscriptions].[PlanNodeQuotas] AS target
USING (VALUES
    (@demoPlanId,  'vehicle-registry', 3),
    (@basicPlanId, 'vehicle-registry', 80),
    (@fullPlanId,  'vehicle-registry', 1000)
) AS source ([PlanId], [NodeKey], [MaxCount])
ON target.[PlanId] = source.[PlanId] AND target.[NodeKey] = source.[NodeKey]
WHEN NOT MATCHED THEN
    INSERT ([PlanId], [NodeKey], [MaxCount]) VALUES (source.[PlanId], source.[NodeKey], source.[MaxCount])
WHEN MATCHED AND target.[MaxCount] <> source.[MaxCount] THEN
    UPDATE SET [MaxCount] = source.[MaxCount];
GO

DECLARE @superRoleId INT = (SELECT [Id] FROM [access].[Roles] WHERE [Code] = 'SuperUser');

MERGE [access].[RoleNodes] AS target
USING (VALUES
    (@superRoleId, '*')
) AS source ([RoleId], [NodeKey])
ON target.[RoleId] = source.[RoleId] AND target.[NodeKey] = source.[NodeKey]
WHEN NOT MATCHED THEN
    INSERT ([RoleId], [NodeKey]) VALUES (source.[RoleId], source.[NodeKey]);
GO
