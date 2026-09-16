-- 030: địa chỉ đối tác, loại dòng lịch trình, nhà hàng mỗi tỉnh, ma trận bay đảo.
-- Idempotent. Chạy sau 029. KHÔNG chạy 025.

IF COL_LENGTH('dbo.DoiTac', 'DiaChi') IS NULL
    ALTER TABLE dbo.DoiTac ADD DiaChi nvarchar(200) NULL;
IF COL_LENGTH('dbo.LichTrinh', 'LoaiDong') IS NULL
    ALTER TABLE dbo.LichTrinh ADD LoaiDong nchar(20) NULL;
IF COL_LENGTH('dbo.LichTrinhDeXuatChiTiet', 'LoaiDong') IS NULL
    ALTER TABLE dbo.LichTrinhDeXuatChiTiet ADD LoaiDong nchar(20) NULL;
GO

UPDATE dt
SET DiaChi = N'Số ' + CAST((ABS(CHECKSUM(dt.MaDoiTac)) % 180) + 1 AS nvarchar(10))
    + N' đường Trần Phú, phường 1, ' + RTRIM(tt.TenTinh)
FROM dbo.DoiTac dt
INNER JOIN dbo.TinhThanh tt ON tt.MaTinh = dt.MaTinh
WHERE dt.DiaChi IS NULL OR LTRIM(RTRIM(dt.DiaChi)) = N'';

UPDATE dt
SET DiaChi = N'Số ' + CAST((ABS(CHECKSUM(dt.MaDoiTac)) % 90) + 1 AS nvarchar(10))
    + N' đường Lê Lợi, ' + RTRIM(ISNULL(kv.TenKhuVuc, N'Việt Nam'))
FROM dbo.DoiTac dt
LEFT JOIN dbo.KhuVuc kv ON kv.MaKhuVuc = dt.MaKhuVuc
WHERE dt.DiaChi IS NULL OR LTRIM(RTRIM(dt.DiaChi)) = N'';
GO

;WITH nums AS (SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5)
INSERT INTO dbo.DoiTac (MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, DiaChi, MaKhuVuc, MaTinh, PhanTramHoaHong, TrangThai)
SELECT
    LEFT(N'DAU' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + REPLICATE(N' ', 20), 20),
    CASE n.n
        WHEN 1 THEN N'Nhà hàng đặc sản ' + RTRIM(t.TenTinh)
        WHEN 2 THEN N'Quán cơm quê ' + RTRIM(t.TenTinh)
        WHEN 3 THEN N'Hải sản / vườn ' + RTRIM(t.TenTinh)
        WHEN 4 THEN N'Lẩu nướng ' + RTRIM(t.TenTinh)
        ELSE N'Quán ăn gia đình ' + RTRIM(t.TenTinh)
    END,
    N'AnUong              ',
    N'Bếp trưởng',
    LEFT(N'0903' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + N'01' + REPLICATE(N' ', 20), 20),
    N'an' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + N'@anam.vn',
    N'Số ' + CAST(n.n * 11 AS nvarchar(10)) + N' đường Nguyễn Huệ, phường 1, ' + RTRIM(t.TenTinh),
    t.MaKhuVuc,
    t.MaTinh,
    CAST(12.00 AS decimal(5,2)),
    N'HoatDong            '
FROM dbo.TinhThanh t
CROSS JOIN nums n
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DoiTac d
    WHERE d.MaDoiTac = LEFT(N'DAU' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + REPLICATE(N' ', 20), 20)
);
GO

;WITH nums AS (SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3)
INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
SELECT
    LEFT(N'SPA' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + CAST(p.n AS nvarchar(1)) + REPLICATE(N' ', 20), 20),
    LEFT(N'DAU' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + REPLICATE(N' ', 20), 20),
    CASE p.n WHEN 1 THEN N'Set ăn sáng' WHEN 2 THEN N'Set ăn trưa' ELSE N'Set ăn tối' END,
    N'suat',
    120000 + n.n * 40000 + p.n * 25000,
    NULL,
    N'Set menu tại ' + RTRIM(t.TenTinh),
    N'HoatDong            '
FROM dbo.TinhThanh t
CROSS JOIN (SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5) n
CROSS JOIN nums p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SanPhamDoiTac s
    WHERE s.MaSanPham = LEFT(N'SPA' + RIGHT(RTRIM(t.MaTinh), 2) + CAST(n.n AS nvarchar(1)) + CAST(p.n AS nvarchar(1)) + REPLICATE(N' ', 20), 20)
);
GO

IF OBJECT_ID('dbo.MatranDiChuyen', 'U') IS NOT NULL
BEGIN
    MERGE dbo.MatranDiChuyen AS target
    USING (VALUES
        (N'TN01', N'TN58', N'MayBay', 165, 1850000),
        (N'TN58', N'TN01', N'MayBay', 165, 1850000),
        (N'TN45', N'TN58', N'MayBay', 75, 1450000),
        (N'TN58', N'TN45', N'MayBay', 75, 1450000),
        (N'TN01', N'TN32', N'MayBay', 90, 1350000),
        (N'TN32', N'TN01', N'MayBay', 90, 1350000),
        (N'TN45', N'TN32', N'MayBay', 80, 1200000),
        (N'TN32', N'TN45', N'MayBay', 80, 1200000),
        (N'TN01', N'TN44', N'MayBay', 110, 1500000),
        (N'TN44', N'TN01', N'MayBay', 110, 1500000),
        (N'TN45', N'TN44', N'MayBay', 55, 950000),
        (N'TN44', N'TN45', N'MayBay', 55, 950000),
        (N'TN01', N'TN37', N'MayBay', 105, 1550000),
        (N'TN37', N'TN01', N'MayBay', 105, 1550000),
        (N'TN45', N'TN37', N'MayBay', 70, 1100000),
        (N'TN37', N'TN45', N'MayBay', 70, 1100000)
    ) AS src(MaTinhDi, MaTinhDen, PhuongTien, SoPhut, ChiPhi)
    ON target.MaTinhDi = LEFT(src.MaTinhDi + REPLICATE(N' ', 20), 20)
       AND target.MaTinhDen = LEFT(src.MaTinhDen + REPLICATE(N' ', 20), 20)
       AND RTRIM(target.PhuongTien) = src.PhuongTien
    WHEN MATCHED THEN UPDATE SET SoPhut = src.SoPhut, ChiPhiUocTinh = src.ChiPhi
    WHEN NOT MATCHED THEN INSERT (MaTinhDi, MaTinhDen, PhuongTien, SoPhut, ChiPhiUocTinh)
        VALUES (LEFT(src.MaTinhDi + REPLICATE(N' ', 20), 20), LEFT(src.MaTinhDen + REPLICATE(N' ', 20), 20),
                LEFT(src.PhuongTien + REPLICATE(N' ', 20), 20), src.SoPhut, src.ChiPhi);
END
GO
