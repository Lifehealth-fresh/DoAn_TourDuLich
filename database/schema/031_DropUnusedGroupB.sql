-- 031_DropUnusedGroupB.sql
-- Idempotent. Xoa 10 bang nhom B (khong dung UI nghiep vu).
-- Khong dung Tour.SLHuongDanVien, QuyenNhanVien, CuocTroChuyen, TinNhanHoTro,
-- AIGoiY, HanhViKhachHang, YeuCauThietKe, LichTrinhDeXuat.
-- Chay 1 lan tren Azure SQL / SSMS. Khong chay 025.

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* Con truoc, cha sau */
IF OBJECT_ID(N'dbo.MediaDanhGiaHdv', N'U') IS NOT NULL
    DROP TABLE dbo.MediaDanhGiaHdv;
IF OBJECT_ID(N'dbo.MediaDanhGiaHDV', N'U') IS NOT NULL
    DROP TABLE dbo.MediaDanhGiaHDV;

IF OBJECT_ID(N'dbo.MediaDanhGiaSanPham', N'U') IS NOT NULL
    DROP TABLE dbo.MediaDanhGiaSanPham;

IF OBJECT_ID(N'dbo.DanhGiaHDV', N'U') IS NOT NULL
    DROP TABLE dbo.DanhGiaHDV;

IF OBJECT_ID(N'dbo.DanhGiaSanPhamDoiTac', N'U') IS NOT NULL
    DROP TABLE dbo.DanhGiaSanPhamDoiTac;

IF OBJECT_ID(N'dbo.LichDanTour', N'U') IS NOT NULL
    DROP TABLE dbo.LichDanTour;

IF OBJECT_ID(N'dbo.TinNhanThietKe', N'U') IS NOT NULL
    DROP TABLE dbo.TinNhanThietKe;

IF OBJECT_ID(N'dbo.HoiThoaiThietKe', N'U') IS NOT NULL
    DROP TABLE dbo.HoiThoaiThietKe;

IF OBJECT_ID(N'dbo.HuongDanVien', N'U') IS NOT NULL
    DROP TABLE dbo.HuongDanVien;

IF OBJECT_ID(N'dbo.Quyen', N'U') IS NOT NULL
    DROP TABLE dbo.Quyen;

IF OBJECT_ID(N'dbo.JobRunLog', N'U') IS NOT NULL
    DROP TABLE dbo.JobRunLog;
GO

/* Xac nhan */
SELECT t.name AS ConLaiNhomB
FROM sys.tables t
WHERE t.name IN (
    N'MediaDanhGiaHdv', N'MediaDanhGiaHDV', N'MediaDanhGiaSanPham',
    N'DanhGiaHDV', N'DanhGiaSanPhamDoiTac', N'LichDanTour',
    N'TinNhanThietKe', N'HoiThoaiThietKe', N'HuongDanVien',
    N'Quyen', N'JobRunLog'
);
-- Ket qua rong = da xoa het.
GO
