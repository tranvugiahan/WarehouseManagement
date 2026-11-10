-- Cập nhật warehouse_db hiện có; có thể chạy lại.
-- Chỉ tạo dbo.refresh_sessions nếu thiếu. Không xóa hoặc sửa dữ liệu hiện có.
-- Chạy bằng SSMS Execute hoặc: sqlcmd -S . -E -b -i warehouse_db_sqlserver.sql
USE [warehouse_db];
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
    THROW 50001, 'Missing dbo.users. Create the base schema first.', 1;

IF OBJECT_ID(N'dbo.refresh_sessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.refresh_sessions (
        id         VARCHAR(32)  NOT NULL CONSTRAINT pk_refresh_sessions PRIMARY KEY,
        user_id    INT          NOT NULL,
        token_hash VARCHAR(64)  NOT NULL,
        expires_at DATETIME2(0) NOT NULL,
        created_at DATETIME2(0) NOT NULL CONSTRAINT df_refresh_sessions_created DEFAULT (SYSUTCDATETIME()),
        revoked_at DATETIME2(0) NULL,
        CONSTRAINT fk_refresh_sessions_user FOREIGN KEY (user_id) REFERENCES dbo.users (id),
        CONSTRAINT uq_refresh_sessions_token_hash UNIQUE (token_hash)
    );
    CREATE INDEX idx_refresh_sessions_user_id ON dbo.refresh_sessions (user_id);
END;
