-- Apply only after the application validates access tokens against refresh_sessions.
-- This script refuses to drop a nonempty table. Review and approve before execution.
USE [warehouse_db];
GO

IF OBJECT_ID(N'dbo.revoked_tokens', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.revoked_tokens)
        THROW 50002, 'dbo.revoked_tokens is not empty; review its rows before dropping.', 1;

    DROP TABLE dbo.revoked_tokens;
END;
