/* 028: hạn sửa đánh giá 5 ngày, media Cloudinary, ảnh giấy tờ, quyền quản lý khách.
   Idempotent. Chạy trên Azure sau 027. Không USE. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.DanhGiaTour', N'ThoiGianSua') IS NULL
    ALTER TABLE dbo.DanhGiaTour ADD ThoiGianSua datetime NULL;
GO

IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'CloudPublicId') IS NULL
    ALTER TABLE dbo.MediaDanhGiaTour ADD CloudPublicId nvarchar(200) NULL;
GO
IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'CloudResourceType') IS NULL
    ALTER TABLE dbo.MediaDanhGiaTour ADD CloudResourceType nvarchar(20) NULL;
GO
IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'Url') IS NOT NULL
    ALTER TABLE dbo.MediaDanhGiaTour ALTER COLUMN Url nvarchar(500) NOT NULL;
GO

IF COL_LENGTH(N'dbo.GiayTo', N'AnhMatTruoc') IS NULL
    ALTER TABLE dbo.GiayTo ADD AnhMatTruoc nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.GiayTo', N'AnhMatSau') IS NULL
    ALTER TABLE dbo.GiayTo ADD AnhMatSau nvarchar(500) NULL;
GO
IF COL_LENGTH(N'dbo.GiayTo', N'CloudPublicIdTruoc') IS NULL
    ALTER TABLE dbo.GiayTo ADD CloudPublicIdTruoc nvarchar(200) NULL;
GO
IF COL_LENGTH(N'dbo.GiayTo', N'CloudPublicIdSau') IS NULL
    ALTER TABLE dbo.GiayTo ADD CloudPublicIdSau nvarchar(200) NULL;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang')
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang')
    ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
    CHECK (ChucNang IN (N'TongQuan', N'Tour', N'Booking', N'UuDai',
        N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia', N'KhachHang', N'TaiKhoan'));
GO

INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT CONVERT(nchar(20), LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20)),
       u.MaUser, N'KhachHang', 1, 1, 1, 0
FROM dbo.NguoiSuDung u
JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
WHERE LTRIM(RTRIM(v.TenVaiTro)) IN (N'Admin', N'Sale')
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = N'KhachHang');
GO

SELECT N'DanhGiaTour.ThoiGianSua' AS Cot, CASE WHEN COL_LENGTH(N'dbo.DanhGiaTour', N'ThoiGianSua') IS NULL THEN 0 ELSE 1 END AS CoCot
UNION ALL
SELECT N'GiayTo.AnhMatTruoc', CASE WHEN COL_LENGTH(N'dbo.GiayTo', N'AnhMatTruoc') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'Quyen KhachHang', COUNT(*) FROM dbo.QuyenNhanVien WHERE ChucNang = N'KhachHang';
GO
