/* Observability for scheduled data jobs. Safe to run on an existing database. */
IF OBJECT_ID(N'dbo.JobRunLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobRunLog
    (
        MaJobRun bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobRunLog PRIMARY KEY,
        TenJob nvarchar(100) NOT NULL,
        ThoiDiemBatDau datetime2 NOT NULL,
        ThoiDiemKetThuc datetime2 NULL,
        SoBanGhi int NOT NULL CONSTRAINT DF_JobRunLog_SoBanGhi DEFAULT (0),
        TrangThai nvarchar(20) NOT NULL,
        Loi nvarchar(2000) NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobRunLog_TenJob_BatDau' AND object_id = OBJECT_ID(N'dbo.JobRunLog'))
    CREATE INDEX IX_JobRunLog_TenJob_BatDau ON dbo.JobRunLog(TenJob, ThoiDiemBatDau DESC);
GO
