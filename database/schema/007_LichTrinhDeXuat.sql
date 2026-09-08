IF COL_LENGTH(N'dbo.YeuCauThietKe', N'LyDoTuChoiBoiSale') IS NULL
BEGIN
    ALTER TABLE dbo.YeuCauThietKe ADD LyDoTuChoiBoiSale nvarchar(max) NULL;
END;
GO

IF OBJECT_ID(N'dbo.LichTrinhDeXuat', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichTrinhDeXuat
    (
        MaDeXuat nchar(20) NOT NULL,
        MaYeuCau nchar(20) NOT NULL,
        ThuTuPhuongAn int NOT NULL,
        TenPhuongAn nvarchar(100) NOT NULL,
        TongTienDuKien int NOT NULL,
        GhiChu nvarchar(500) NULL,
        TrangThai nchar(20) NOT NULL CONSTRAINT DF_LichTrinhDeXuat_TrangThai DEFAULT N'DeXuat',
        NgayTao datetime2 NOT NULL CONSTRAINT DF_LichTrinhDeXuat_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_LichTrinhDeXuat PRIMARY KEY (MaDeXuat),
        CONSTRAINT FK_LichTrinhDeXuat_YeuCau FOREIGN KEY (MaYeuCau)
            REFERENCES dbo.YeuCauThietKe(MaYeuCau) ON DELETE CASCADE,
        CONSTRAINT CK_LichTrinhDeXuat_TrangThai CHECK (TrangThai IN (N'DeXuat', N'DaChon', N'KhongChon'))
    );
END;
GO

IF OBJECT_ID(N'dbo.LichTrinhDeXuatChiTiet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichTrinhDeXuatChiTiet
    (
        MaChiTiet nchar(20) NOT NULL,
        MaDeXuat nchar(20) NOT NULL,
        NgayThu int NOT NULL,
        ThuTuTrongNgay int NOT NULL,
        MaDThamQuan nchar(20) NULL,
        MaSanPham nchar(20) NULL,
        SoLuong int NOT NULL,
        DonGia int NOT NULL,
        ThanhTien int NOT NULL,
        Mota nvarchar(500) NULL,
        CONSTRAINT PK_LichTrinhDeXuatChiTiet PRIMARY KEY (MaChiTiet),
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_DeXuat FOREIGN KEY (MaDeXuat)
            REFERENCES dbo.LichTrinhDeXuat(MaDeXuat) ON DELETE CASCADE,
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_Diem FOREIGN KEY (MaDThamQuan)
            REFERENCES dbo.DiemThamQuan(MaDThamQuan),
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_SanPham FOREIGN KEY (MaSanPham)
            REFERENCES dbo.SanPhamDoiTac(MaSanPham)
    );
END;
GO
