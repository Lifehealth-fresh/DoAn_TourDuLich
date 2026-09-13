/* Phân quyền theo từng tài khoản nhân viên (Sale/Admin).
   Mỗi dòng = một chức năng: Them / Sua / Xoa / ToanQuyen.
   Không tích ô nào = không được dùng chức năng đó.
   Script idempotent: tạo bảng nếu chưa có, bổ sung quyền cho tài khoản sẵn có. */

IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuyenNhanVien
    (
        MaQuyen nchar(20) NOT NULL CONSTRAINT PK_QuyenNhanVien PRIMARY KEY,
        MaUser nchar(20) NOT NULL,
        ChucNang nvarchar(40) NOT NULL,
        Them bit NOT NULL CONSTRAINT DF_QuyenNhanVien_Them DEFAULT (0),
        Sua bit NOT NULL CONSTRAINT DF_QuyenNhanVien_Sua DEFAULT (0),
        Xoa bit NOT NULL CONSTRAINT DF_QuyenNhanVien_Xoa DEFAULT (0),
        ToanQuyen bit NOT NULL CONSTRAINT DF_QuyenNhanVien_ToanQuyen DEFAULT (0),
        CONSTRAINT UQ_QuyenNhanVien_User_ChucNang UNIQUE (MaUser, ChucNang),
        CONSTRAINT FK_QuyenNhanVien_NguoiSuDung FOREIGN KEY (MaUser)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT CK_QuyenNhanVien_ChucNang CHECK (RTRIM(ChucNang) IN (
            N'TongQuan', N'Tour', N'Booking', N'UuDai',
            N'DiemThamQuan', N'DoiTac', N'ThietKe', N'TaiKhoan'))
    );
END
GO

IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_QuyenNhanVien_MaUser'
          AND object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
BEGIN
    CREATE INDEX IX_QuyenNhanVien_MaUser ON dbo.QuyenNhanVien (MaUser);
END
GO

DECLARE @Modules TABLE (ChucNang nvarchar(40) NOT NULL PRIMARY KEY);
INSERT INTO @Modules (ChucNang)
VALUES (N'TongQuan'), (N'Tour'), (N'Booking'), (N'UuDai'),
       (N'DiemThamQuan'), (N'DoiTac'), (N'ThietKe'), (N'TaiKhoan');

/* Admin sẵn có: toàn quyền mọi chức năng. */
INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT
    CONVERT(nchar(20), LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20)),
    u.MaUser,
    m.ChucNang,
    1, 1, 1, 1
FROM dbo.NguoiSuDung u
INNER JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
CROSS JOIN @Modules m
WHERE RTRIM(v.TenVaiTro) = N'Admin'
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = m.ChucNang);

/* Sale sẵn có: toàn quyền nghiệp vụ, không quản lý tài khoản. */
INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT
    CONVERT(nchar(20), LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 20)),
    u.MaUser,
    m.ChucNang,
    CASE WHEN m.ChucNang = N'TaiKhoan' THEN 0 ELSE 1 END,
    CASE WHEN m.ChucNang = N'TaiKhoan' THEN 0 ELSE 1 END,
    CASE WHEN m.ChucNang = N'TaiKhoan' THEN 0 ELSE 1 END,
    CASE WHEN m.ChucNang = N'TaiKhoan' THEN 0 ELSE 1 END
FROM dbo.NguoiSuDung u
INNER JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
CROSS JOIN @Modules m
WHERE RTRIM(v.TenVaiTro) = N'Sale'
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = m.ChucNang);
GO
