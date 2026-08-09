SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('users.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE users.RefreshTokens
    (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        UserId              BIGINT        NOT NULL,
        TokenHash           VARBINARY(32) NOT NULL,
        ExpiresAtUtc        DATETIME2(3)  NOT NULL,
        RevokedAtUtc        DATETIME2(3)  NULL,
        ReplacedByTokenHash VARBINARY(32) NULL,
        DeviceInfo          NVARCHAR(200) NULL,
        CreatedAtUtc        DATETIME2(3)  NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES users.Users (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID('users.RefreshTokens'))
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON users.RefreshTokens (TokenHash);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID('users.RefreshTokens'))
    CREATE INDEX IX_RefreshTokens_UserId ON users.RefreshTokens (UserId, ExpiresAtUtc DESC);
GO
