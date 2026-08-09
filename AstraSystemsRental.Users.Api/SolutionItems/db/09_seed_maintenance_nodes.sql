USE [AstraSystemsRental];
GO

MERGE [access].[Nodes] AS target
USING (VALUES
    ('maintenance-tracking',  N'Control de Recorrido',      N'<circle cx="12" cy="12" r="8.25" /><path d="M12 7.5V12l3 1.75" />', 'Maintenance/Index',        40),
    ('maintenance-routines',  N'Rutinas de Mantenimiento',  N'<path d="M4.5 6.75h15" /><path d="M4.5 12h15" /><path d="M4.5 17.25h9" /><circle cx="18" cy="17.25" r="2.25" />', 'Maintenance/Routines',     45),
    ('workshop-reservations', N'Reservas de Taller',        N'<rect x="3.75" y="5.25" width="16.5" height="15" rx="1.75" /><path d="M8.25 3v4.5" /><path d="M15.75 3v4.5" /><path d="M3.75 10.5h16.5" />', 'Maintenance/Reservations', 50)
) AS source ([Key], [Label], [Icon], [Route], [SortOrder])
ON target.[Key] = source.[Key]
WHEN NOT MATCHED THEN
    INSERT ([Key], [Label], [Icon], [Route], [SortOrder])
    VALUES (source.[Key], source.[Label], source.[Icon], source.[Route], source.[SortOrder]);
GO

DECLARE @demoPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Demo');
DECLARE @basicPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Basic');
DECLARE @fullPlanId INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Full');

MERGE [subscriptions].[PlanNodes] AS target
USING (VALUES
    (@demoPlanId,  'maintenance-tracking'),
    (@basicPlanId, 'maintenance-tracking'),
    (@basicPlanId, 'maintenance-routines'),
    (@basicPlanId, 'workshop-reservations'),
    (@fullPlanId,  'maintenance-tracking'),
    (@fullPlanId,  'maintenance-routines'),
    (@fullPlanId,  'workshop-reservations')
) AS source ([PlanId], [NodeKey])
ON target.[PlanId] = source.[PlanId] AND target.[NodeKey] = source.[NodeKey]
WHEN NOT MATCHED THEN
    INSERT ([PlanId], [NodeKey]) VALUES (source.[PlanId], source.[NodeKey]);
GO

DECLARE @demoPlanIdQ INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Demo');
DECLARE @basicPlanIdQ INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Basic');
DECLARE @fullPlanIdQ INT = (SELECT [Id] FROM [subscriptions].[Plans] WHERE [Code] = 'Full');

MERGE [subscriptions].[PlanNodeQuotas] AS target
USING (VALUES
    (@basicPlanIdQ, 'maintenance-routines',  10),
    (@fullPlanIdQ,  'maintenance-routines',  100),
    (@basicPlanIdQ, 'workshop-reservations', 5),
    (@fullPlanIdQ,  'workshop-reservations', 50)
) AS source ([PlanId], [NodeKey], [MaxCount])
ON target.[PlanId] = source.[PlanId] AND target.[NodeKey] = source.[NodeKey]
WHEN NOT MATCHED THEN
    INSERT ([PlanId], [NodeKey], [MaxCount]) VALUES (source.[PlanId], source.[NodeKey], source.[MaxCount])
WHEN MATCHED AND target.[MaxCount] <> source.[MaxCount] THEN
    UPDATE SET [MaxCount] = source.[MaxCount];
GO
