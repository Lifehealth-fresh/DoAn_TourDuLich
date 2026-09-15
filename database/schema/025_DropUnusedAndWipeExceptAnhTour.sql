/* 025: Xóa bảng không dùng trong sản phẩm + xóa dữ liệu (GIỮ Ảnh tour).
   Chạy trên Azure SQL TourDuLich, SSMS, database đang dùng (không USE).
   Giữ: dbo.AnhTour + các Tour đang được ảnh trỏ tới, VaiTro, KhuVuc.
   Bảng chết: Quyen (thay bằng QuyenNhanVien), ThongBao (API/UI không ghi). */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ---- DROP bảng không thuộc luồng sản phẩm ---- */
IF OBJECT_ID(N'dbo.Quyen', N'U') IS NOT NULL
    DROP TABLE dbo.Quyen;
GO
IF OBJECT_ID(N'dbo.ThongBao', N'U') IS NOT NULL
    DROP TABLE dbo.ThongBao;
GO

BEGIN TRANSACTION;

DELETE FROM dbo.TinNhanThietKe;
DELETE FROM dbo.HoiThoaiThietKe;
DELETE FROM dbo.LichTrinhDeXuatChiTiet;
DELETE FROM dbo.LichTrinhDeXuat;
DELETE FROM dbo.YeuCauThietKe;

DELETE FROM dbo.MediaDanhGiaTour;
DELETE FROM dbo.MediaDanhGiaHdv;
DELETE FROM dbo.MediaDanhGiaSanPham;
DELETE FROM dbo.DanhGiaTour;
DELETE FROM dbo.DanhGiaHDV;
DELETE FROM dbo.DanhGiaSanPhamDoiTac;

DELETE FROM dbo.DatDichVu_KhuyenMai;
DELETE FROM dbo.ThanhToan;
DELETE FROM dbo.HopDong;
DELETE FROM dbo.DatDichVu;

DELETE FROM dbo.RefreshToken;
DELETE FROM dbo.QuyenNhanVien;
DELETE FROM dbo.HanhViKhachHang;
DELETE FROM dbo.DanhSachYeuThich;
DELETE FROM dbo.AIGoiY;

DELETE FROM dbo.LichDanTour;
DELETE FROM dbo.LichTrinh;
DELETE FROM dbo.LichKhoiHanh;

DELETE FROM dbo.KM_Tour;
DELETE FROM dbo.DieuKienKM;
DELETE FROM dbo.KhuyenMai;

DELETE FROM dbo.JobRunLog;
DELETE FROM dbo.GiayTo;
DELETE FROM dbo.KhachHang;
DELETE FROM dbo.NguoiSuDung;

DELETE FROM dbo.SanPhamDoiTac;
DELETE FROM dbo.DoiTac;
DELETE FROM dbo.DiemThamQuan;
DELETE FROM dbo.MatranDiChuyen;
DELETE FROM dbo.TinhThanhAlias;
DELETE FROM dbo.TinhThanh;

DELETE FROM dbo.HuongDanVien;
DELETE FROM dbo.NhomKhuyenMai;

/* Tour: xóa tour KHÔNG bị AnhTour giữ. Ảnh Cloudinary giữ nguyên. */
DELETE FROM dbo.Tour
WHERE NOT EXISTS (SELECT 1 FROM dbo.AnhTour a WHERE a.MaTour = dbo.Tour.MaTour);

COMMIT;
GO

/* KhuVuc / VaiTro: không xóa (FK + 3 miền / 3 vai trò trong code). */
UPDATE dbo.KhuVuc SET
    TenKhuVuc = CASE RTRIM(MaKhuVuc)
        WHEN N'KV001' THEN N'Miền Bắc'
        WHEN N'KV002' THEN N'Miền Trung'
        ELSE N'Miền Nam' END,
    QuocGia = N'Việt Nam',
    ViDo = CASE RTRIM(MaKhuVuc)
        WHEN N'KV001' THEN CAST(21.028511 AS decimal(9,6))
        WHEN N'KV002' THEN CAST(16.054407 AS decimal(9,6))
        ELSE CAST(10.823099 AS decimal(9,6)) END,
    KinhDo = CASE RTRIM(MaKhuVuc)
        WHEN N'KV001' THEN CAST(105.804817 AS decimal(9,6))
        WHEN N'KV002' THEN CAST(108.202164 AS decimal(9,6))
        ELSE CAST(106.629662 AS decimal(9,6)) END,
    MuiGio = N'UTC+07:00',
    TrangThai = N'HoatDong';

UPDATE dbo.VaiTro SET Mota = N'Quản trị toàn hệ thống' WHERE MaVaiTro = 1 AND (Mota IS NULL OR Mota = N'');
UPDATE dbo.VaiTro SET Mota = N'Nhân viên kinh doanh' WHERE MaVaiTro = 2 AND (Mota IS NULL OR Mota = N'');
UPDATE dbo.VaiTro SET Mota = N'Khách hàng' WHERE MaVaiTro = 3 AND (Mota IS NULL OR Mota = N'');
GO
