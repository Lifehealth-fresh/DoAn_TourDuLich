/* Metadata required to manage public Cloudinary assets after upload. */
IF COL_LENGTH(N'dbo.AnhTour', N'CloudPublicId') IS NULL
    ALTER TABLE dbo.AnhTour ADD CloudPublicId nvarchar(255) NULL;
GO

IF COL_LENGTH(N'dbo.AnhTour', N'CloudResourceType') IS NULL
    ALTER TABLE dbo.AnhTour ADD CloudResourceType nvarchar(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AnhTour_CloudPublicId' AND object_id = OBJECT_ID(N'dbo.AnhTour'))
    CREATE INDEX IX_AnhTour_CloudPublicId ON dbo.AnhTour(CloudPublicId) WHERE CloudPublicId IS NOT NULL;
GO
