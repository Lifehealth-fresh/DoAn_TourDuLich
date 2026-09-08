-- Adds media type and a provider-neutral URL column while retaining ImageURL
-- for backward compatibility with the original schema and existing clients.
-- Dynamic batches are intentional: SQL Server binds column names before executing
-- ALTER TABLE in the same batch.
IF COL_LENGTH(N'dbo.AnhTour', N'LoaiMedia') IS NULL
BEGIN
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ADD LoaiMedia nvarchar(10) NULL
        CONSTRAINT DF_AnhTour_LoaiMedia DEFAULT N''Anh'';';
    EXEC sys.sp_executesql N'UPDATE dbo.AnhTour SET LoaiMedia = N''Anh'' WHERE LoaiMedia IS NULL;';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ALTER COLUMN LoaiMedia nvarchar(10) NOT NULL;';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AnhTour_LoaiMedia')
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.AnhTour ADD CONSTRAINT CK_AnhTour_LoaiMedia
            CHECK (RTRIM(LoaiMedia) IN (N''Anh'', N''Video''));';
END;
GO

IF COL_LENGTH(N'dbo.AnhTour', N'Url') IS NULL
BEGIN
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ADD Url nvarchar(300) NULL;';
    EXEC sys.sp_executesql N'UPDATE dbo.AnhTour SET Url = ImageURL WHERE Url IS NULL;';
END;
GO
