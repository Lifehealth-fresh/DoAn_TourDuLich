/* Phiên đăng nhập có thể thu hồi. Chạy sau 020. Idempotent. */
IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        MaRefresh nchar(20) NOT NULL CONSTRAINT PK_RefreshToken PRIMARY KEY,
        MaUser nchar(20) NOT NULL,
        TokenHash char(64) NOT NULL,
        HetHan datetime2 NOT NULL,
        ThuHoiLuc datetime2 NULL,
        TaoLuc datetime2 NOT NULL CONSTRAINT DF_RefreshToken_TaoLuc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RefreshToken_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshToken_NguoiSuDung FOREIGN KEY (MaUser)
            REFERENCES dbo.NguoiSuDung (MaUser)
    );
END
GO

IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_RefreshToken_MaUser'
          AND object_id = OBJECT_ID(N'dbo.RefreshToken'))
BEGIN
    CREATE INDEX IX_RefreshToken_MaUser ON dbo.RefreshToken (MaUser);
END
GO
