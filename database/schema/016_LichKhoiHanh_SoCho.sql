-- SQL Server / Azure SQL. Run in the application's existing database.
-- NULL retains Tour.Slkhach as the default capacity.
IF COL_LENGTH(N'dbo.LichKhoiHanh', N'SoCho') IS NULL
BEGIN
    ALTER TABLE dbo.LichKhoiHanh ADD SoCho INT NULL;
END;
GO
