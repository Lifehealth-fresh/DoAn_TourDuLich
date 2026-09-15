/* Chạy nếu 028 cũ đã thêm cột nhưng INSERT quyền bị lỗi MaQuyen NULL. Idempotent. */
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO
ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
CHECK (RTRIM(ChucNang) IN (N'TongQuan', N'Tour', N'Booking', N'UuDai',
    N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia', N'KhachHang', N'TaiKhoan'));
GO
INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT CONVERT(nchar(20), LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20)),
       u.MaUser, N'KhachHang', 1, 1, 1, 0
FROM dbo.NguoiSuDung u
INNER JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
WHERE RTRIM(v.TenVaiTro) IN (N'Admin', N'Sale')
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = N'KhachHang');
GO
SELECT COUNT(*) AS SoQuyenKhachHang FROM dbo.QuyenNhanVien WHERE ChucNang = N'KhachHang';
GO
