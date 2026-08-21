/*
    Gỡ ràng buộc duy nhất trên KhachHang.MaUser để một tài khoản
    có thể quản lý nhiều hồ sơ khách hàng.

    Chạy thủ công bằng SSMS trên đúng database của đồ án.
*/

ALTER TABLE dbo.KhachHang
DROP CONSTRAINT UQ_KhachHang_MaUser;
GO
