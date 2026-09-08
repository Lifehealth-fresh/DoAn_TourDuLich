/* Seed vai trò, có thể chạy lại an toàn. */

IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Admin')
    INSERT INTO dbo.VaiTro (MaVaiTro, TenVaiTro, Mota)
    VALUES (1, N'Admin', N'Quản trị toàn hệ thống');

IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Sale')
    INSERT INTO dbo.VaiTro (MaVaiTro, TenVaiTro, Mota)
    VALUES (2, N'Sale', N'Nhân viên kinh doanh');

IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'KhachHang')
    INSERT INTO dbo.VaiTro (MaVaiTro, TenVaiTro, Mota)
    VALUES (3, N'KhachHang', N'Khách hàng');
GO
