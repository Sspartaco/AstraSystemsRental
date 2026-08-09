USE [AstraSystemsRental];
GO

IF OBJECT_ID(N'subscriptions.PlanNodeQuotas', N'U') IS NULL
BEGIN
    CREATE TABLE [subscriptions].[PlanNodeQuotas]
    (
        [PlanId]   INT         NOT NULL,
        [NodeKey]  VARCHAR(80) NOT NULL,
        [MaxCount] INT         NOT NULL,
        CONSTRAINT [PK_PlanNodeQuotas] PRIMARY KEY CLUSTERED ([PlanId], [NodeKey]),
        CONSTRAINT [FK_PlanNodeQuotas_Plans] FOREIGN KEY ([PlanId]) REFERENCES [subscriptions].[Plans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlanNodeQuotas_Nodes] FOREIGN KEY ([NodeKey]) REFERENCES [access].[Nodes] ([Key]),
        CONSTRAINT [CK_PlanNodeQuotas_MaxCount] CHECK ([MaxCount] >= 0)
    );
END
GO

IF OBJECT_ID(N'subscriptions.PlanNodeUsageCounters', N'U') IS NULL
BEGIN
    CREATE TABLE [subscriptions].[PlanNodeUsageCounters]
    (
        [OwnerType]    VARCHAR(20) NOT NULL,
        [OwnerId]      BIGINT      NOT NULL,
        [NodeKey]      VARCHAR(80) NOT NULL,
        [CurrentCount] INT         NOT NULL CONSTRAINT [DF_PlanNodeUsageCounters_CurrentCount] DEFAULT (0),
        CONSTRAINT [PK_PlanNodeUsageCounters] PRIMARY KEY CLUSTERED ([OwnerType], [OwnerId], [NodeKey]),
        CONSTRAINT [CK_PlanNodeUsageCounters_OwnerType] CHECK ([OwnerType] IN ('User', 'Company')),
        CONSTRAINT [CK_PlanNodeUsageCounters_CurrentCount] CHECK ([CurrentCount] >= 0)
    );
END
GO
