IF OBJECT_ID(N'dbo.MediaDanhGiaTour', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaDanhGiaTour
    (
        MaMedia nchar(20) NOT NULL,
        MaDanhGiaTour nchar(20) NOT NULL,
        LoaiMedia nvarchar(10) NOT NULL,
        Url nvarchar(300) NOT NULL,
        ThuTu int NULL,
        CONSTRAINT PK_MediaDanhGiaTour PRIMARY KEY (MaMedia),
        CONSTRAINT FK_MediaDanhGiaTour_DanhGiaTour FOREIGN KEY (MaDanhGiaTour)
            REFERENCES dbo.DanhGiaTour(MaDanhGiaTour) ON DELETE CASCADE,
        CONSTRAINT CK_MediaDanhGiaTour_LoaiMedia CHECK (LoaiMedia IN (N'Anh', N'Video'))
    );
END;
GO

IF OBJECT_ID(N'dbo.MediaDanhGiaHdv', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaDanhGiaHdv
    (
        MaMedia nchar(20) NOT NULL,
        MaDanhGiaHdv nchar(20) NOT NULL,
        LoaiMedia nvarchar(10) NOT NULL,
        Url nvarchar(300) NOT NULL,
        ThuTu int NULL,
        CONSTRAINT PK_MediaDanhGiaHdv PRIMARY KEY (MaMedia),
        CONSTRAINT FK_MediaDanhGiaHdv_DanhGiaHdv FOREIGN KEY (MaDanhGiaHdv)
            REFERENCES dbo.DanhGiaHDV(MaDanhGiaHDV) ON DELETE CASCADE,
        CONSTRAINT CK_MediaDanhGiaHdv_LoaiMedia CHECK (LoaiMedia IN (N'Anh', N'Video'))
    );
END;
GO

IF OBJECT_ID(N'dbo.MediaDanhGiaSanPham', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaDanhGiaSanPham
    (
        MaMedia nchar(20) NOT NULL,
        MaDanhGia nchar(20) NOT NULL,
        LoaiMedia nvarchar(10) NOT NULL,
        Url nvarchar(300) NOT NULL,
        ThuTu int NULL,
        CONSTRAINT PK_MediaDanhGiaSanPham PRIMARY KEY (MaMedia),
        CONSTRAINT FK_MediaDanhGiaSanPham_DanhGia FOREIGN KEY (MaDanhGia)
            REFERENCES dbo.DanhGiaSanPhamDoiTac(MaDanhGia) ON DELETE CASCADE,
        CONSTRAINT CK_MediaDanhGiaSanPham_LoaiMedia CHECK (LoaiMedia IN (N'Anh', N'Video'))
    );
END;
GO
