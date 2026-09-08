/* Non-clustered indexes cho các truy vấn lọc/join thường xuyên. Chạy lại an toàn. */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_MaUser' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_MaUser ON dbo.DatDichVu(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_MaTour' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_MaTour ON dbo.DatDichVu(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_MaKhoiHanh' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_MaKhoiHanh ON dbo.DatDichVu(MaKhoiHanh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_TrangThai' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_TrangThai ON dbo.DatDichVu(TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThanhToan_MaBooking' AND object_id = OBJECT_ID(N'dbo.ThanhToan')) CREATE INDEX IX_ThanhToan_MaBooking ON dbo.ThanhToan(MaBooking);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThanhToan_TrangThai' AND object_id = OBJECT_ID(N'dbo.ThanhToan')) CREATE INDEX IX_ThanhToan_TrangThai ON dbo.ThanhToan(TrangThai);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GiayTo_MaKhachHang' AND object_id = OBJECT_ID(N'dbo.GiayTo')) CREATE INDEX IX_GiayTo_MaKhachHang ON dbo.GiayTo(MaKhachHang);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KhachHang_MaUser' AND object_id = OBJECT_ID(N'dbo.KhachHang')) CREATE INDEX IX_KhachHang_MaUser ON dbo.KhachHang(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HanhViKhachHang_MaUser' AND object_id = OBJECT_ID(N'dbo.HanhViKhachHang')) CREATE INDEX IX_HanhViKhachHang_MaUser ON dbo.HanhViKhachHang(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HanhViKhachHang_MaUser_ThoiGian' AND object_id = OBJECT_ID(N'dbo.HanhViKhachHang')) CREATE INDEX IX_HanhViKhachHang_MaUser_ThoiGian ON dbo.HanhViKhachHang(MaUser, ThoiGian DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HanhViKhachHang_MaTour' AND object_id = OBJECT_ID(N'dbo.HanhViKhachHang')) CREATE INDEX IX_HanhViKhachHang_MaTour ON dbo.HanhViKhachHang(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhSachYeuThich_MaUser' AND object_id = OBJECT_ID(N'dbo.DanhSachYeuThich')) CREATE INDEX IX_DanhSachYeuThich_MaUser ON dbo.DanhSachYeuThich(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhSachYeuThich_MaTour' AND object_id = OBJECT_ID(N'dbo.DanhSachYeuThich')) CREATE INDEX IX_DanhSachYeuThich_MaTour ON dbo.DanhSachYeuThich(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGiaTour_MaTour' AND object_id = OBJECT_ID(N'dbo.DanhGiaTour')) CREATE INDEX IX_DanhGiaTour_MaTour ON dbo.DanhGiaTour(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGiaHDV_MaHDV' AND object_id = OBJECT_ID(N'dbo.DanhGiaHDV')) CREATE INDEX IX_DanhGiaHDV_MaHDV ON dbo.DanhGiaHDV(MaHDV);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGiaSanPhamDoiTac_MaSanPham' AND object_id = OBJECT_ID(N'dbo.DanhGiaSanPhamDoiTac')) CREATE INDEX IX_DanhGiaSanPhamDoiTac_MaSanPham ON dbo.DanhGiaSanPhamDoiTac(MaSanPham);
-- Enforce one review per customer and reviewed object. Application-level checks
-- provide a friendly 409; these unique indexes close the concurrent-request race.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DanhGiaTour_MaUser_MaTour' AND object_id = OBJECT_ID(N'dbo.DanhGiaTour')) CREATE UNIQUE INDEX UX_DanhGiaTour_MaUser_MaTour ON dbo.DanhGiaTour(MaUser, MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DanhGiaHDV_MaUser_MaHDV' AND object_id = OBJECT_ID(N'dbo.DanhGiaHDV')) CREATE UNIQUE INDEX UX_DanhGiaHDV_MaUser_MaHDV ON dbo.DanhGiaHDV(MaUser, MaHDV);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DanhGiaSanPhamDoiTac_MaUser_MaSanPham' AND object_id = OBJECT_ID(N'dbo.DanhGiaSanPhamDoiTac')) CREATE UNIQUE INDEX UX_DanhGiaSanPhamDoiTac_MaUser_MaSanPham ON dbo.DanhGiaSanPhamDoiTac(MaUser, MaSanPham);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThietKe_MaUser' AND object_id = OBJECT_ID(N'dbo.YeuCauThietKe')) CREATE INDEX IX_YeuCauThietKe_MaUser ON dbo.YeuCauThietKe(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThietKe_MaTourTao' AND object_id = OBJECT_ID(N'dbo.YeuCauThietKe')) CREATE INDEX IX_YeuCauThietKe_MaTourTao ON dbo.YeuCauThietKe(MaTourTao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichTrinh_MaTour' AND object_id = OBJECT_ID(N'dbo.LichTrinh')) CREATE INDEX IX_LichTrinh_MaTour ON dbo.LichTrinh(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichKhoiHanh_MaTour' AND object_id = OBJECT_ID(N'dbo.LichKhoiHanh')) CREATE INDEX IX_LichKhoiHanh_MaTour ON dbo.LichKhoiHanh(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichDanTour_MaKhoiHanh' AND object_id = OBJECT_ID(N'dbo.LichDanTour')) CREATE INDEX IX_LichDanTour_MaKhoiHanh ON dbo.LichDanTour(MaKhoiHanh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIGoiY_MaUser' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE INDEX IX_AIGoiY_MaUser ON dbo.AIGoiY(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIGoiY_MaUser_NgayGoiY' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE INDEX IX_AIGoiY_MaUser_NgayGoiY ON dbo.AIGoiY(MaUser, NgayGoiY DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_MaUser_NgayDat' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_MaUser_NgayDat ON dbo.DatDichVu(MaUser, NgayDat DESC);
-- One current recommendation per user/tour. ASP.NET replaces a user's set in
-- one transaction; this index also rejects concurrent or malformed duplicates.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AIGoiY_MaUser_MaTour' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE UNIQUE INDEX UX_AIGoiY_MaUser_MaTour ON dbo.AIGoiY(MaUser, MaTour);
GO
