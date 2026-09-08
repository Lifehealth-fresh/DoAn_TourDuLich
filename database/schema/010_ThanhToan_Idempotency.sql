/* Idempotency key for safe payment retries. Run after 001-009 on existing DBs. */
IF COL_LENGTH(N'dbo.ThanhToan', N'IdempotencyKey') IS NULL
    ALTER TABLE dbo.ThanhToan ADD IdempotencyKey nvarchar(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ThanhToan_MaBooking_IdempotencyKey'
               AND object_id = OBJECT_ID(N'dbo.ThanhToan'))
    CREATE UNIQUE INDEX UX_ThanhToan_MaBooking_IdempotencyKey
        ON dbo.ThanhToan(MaBooking, IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
GO
