/* 029: hồ sơ nhân viên, xóa tài khoản (VoHieu), chat khách–admin, module HoTro.
   Idempotent. Chạy sau 028. Không USE. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.NguoiSuDung', N'TrangThai') IS NULL
    ALTER TABLE dbo.NguoiSuDung ADD TrangThai nchar(20) NOT NULL
        CONSTRAINT DF_NguoiSuDung_TrangThai DEFAULT (N'HoatDong');
GO

IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhanVien
    (
        MaNhanVien nchar(20) NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,
        MaUser nchar(20) NOT NULL CONSTRAINT UQ_NhanVien_MaUser UNIQUE,
        Ho nvarchar(50) NOT NULL,
        Ten nvarchar(50) NOT NULL,
        SoCccd nvarchar(20) NOT NULL,
        ChucVu nvarchar(40) NOT NULL,
        CONSTRAINT FK_NhanVien_NguoiSuDung FOREIGN KEY (MaUser)
            REFERENCES dbo.NguoiSuDung (MaUser)
    );
    CREATE INDEX IX_NhanVien_SoCccd ON dbo.NhanVien (SoCccd);
END
GO

IF OBJECT_ID(N'dbo.CuocTroChuyen', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CuocTroChuyen
    (
        MaCuoc nchar(20) NOT NULL CONSTRAINT PK_CuocTroChuyen PRIMARY KEY,
        MaUserKhach nchar(20) NOT NULL,
        MaUserNhanVien nchar(20) NULL,
        TieuDe nvarchar(200) NOT NULL,
        TrangThai nvarchar(20) NOT NULL CONSTRAINT DF_CuocTroChuyen_TrangThai DEFAULT (N'Mo'),
        ThoiGianTao datetime NOT NULL CONSTRAINT DF_CuocTroChuyen_Tao DEFAULT (SYSUTCDATETIME()),
        ThoiGianCapNhat datetime NOT NULL CONSTRAINT DF_CuocTroChuyen_CapNhat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CuocTroChuyen_Khach FOREIGN KEY (MaUserKhach)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT FK_CuocTroChuyen_NhanVien FOREIGN KEY (MaUserNhanVien)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT CK_CuocTroChuyen_TrangThai CHECK (RTRIM(TrangThai) IN (N'Mo', N'Dong'))
    );
    CREATE INDEX IX_CuocTroChuyen_Khach ON dbo.CuocTroChuyen (MaUserKhach, ThoiGianCapNhat DESC);
    CREATE INDEX IX_CuocTroChuyen_NV ON dbo.CuocTroChuyen (MaUserNhanVien, ThoiGianCapNhat DESC);
END
GO

IF OBJECT_ID(N'dbo.TinNhanHoTro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinNhanHoTro
    (
        MaTinNhan nchar(20) NOT NULL CONSTRAINT PK_TinNhanHoTro PRIMARY KEY,
        MaCuoc nchar(20) NOT NULL,
        MaUserGui nchar(20) NOT NULL,
        VaiTroGui nvarchar(20) NOT NULL,
        NoiDung nvarchar(2000) NOT NULL,
        ThoiGian datetime NOT NULL CONSTRAINT DF_TinNhanHoTro_ThoiGian DEFAULT (SYSUTCDATETIME()),
        DaDoc bit NOT NULL CONSTRAINT DF_TinNhanHoTro_DaDoc DEFAULT (0),
        CONSTRAINT FK_TinNhanHoTro_Cuoc FOREIGN KEY (MaCuoc)
            REFERENCES dbo.CuocTroChuyen (MaCuoc) ON DELETE CASCADE,
        CONSTRAINT FK_TinNhanHoTro_User FOREIGN KEY (MaUserGui)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT CK_TinNhanHoTro_VaiTro CHECK (RTRIM(VaiTroGui) IN (N'KhachHang', N'NhanVien'))
    );
    CREATE INDEX IX_TinNhanHoTro_Cuoc ON dbo.TinNhanHoTro (MaCuoc, ThoiGian);
END
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO
IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
    CHECK (RTRIM(ChucNang) IN (
        N'TongQuan', N'Tour', N'Booking', N'UuDai',
        N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia',
        N'KhachHang', N'TaiKhoan', N'HoTro'));
GO

INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT CONVERT(nchar(20), LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20)),
       u.MaUser, N'HoTro', 1, 1, 0, 0
FROM dbo.NguoiSuDung u
INNER JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
WHERE RTRIM(v.TenVaiTro) IN (N'Admin', N'Sale')
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = N'HoTro');
GO

SELECT N'NhanVien' AS Muc, CASE WHEN OBJECT_ID(N'dbo.NhanVien') IS NULL THEN 0 ELSE 1 END AS GiaTri
UNION ALL
SELECT N'CuocTroChuyen', CASE WHEN OBJECT_ID(N'dbo.CuocTroChuyen') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'TinNhanHoTro', CASE WHEN OBJECT_ID(N'dbo.TinNhanHoTro') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'Quyen HoTro', COUNT(*) FROM dbo.QuyenNhanVien WHERE ChucNang = N'HoTro';
GO
