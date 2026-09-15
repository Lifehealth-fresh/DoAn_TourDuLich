-- 023: chat tự thiết kế, alias tỉnh, ma trận di chuyển, giờ lịch trình.
-- Không xóa user / booking / thanh toán. Chạy trên database đang dùng (Azure SQL).
-- Idempotent: có thể chạy lại.

-- ---------------------------------------------------------------------------
-- OPTIONAL RESET (bỏ comment nếu muốn xóa đề xuất/chat cũ, KHÔNG đụng vé)
-- ---------------------------------------------------------------------------
/*
DELETE FROM dbo.TinNhanThietKe;
DELETE FROM dbo.HoiThoaiThietKe;
DELETE FROM dbo.LichTrinhDeXuatChiTiet;
DELETE FROM dbo.LichTrinhDeXuat;
*/

IF COL_LENGTH(N'dbo.YeuCauThietKe', N'TenChuyenDi') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD TenChuyenDi nvarchar(150) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'MucDich') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD MucDich nvarchar(30) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'MaTinhXuatPhat') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD MaTinhXuatPhat nchar(20) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'MaTinhDen') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD MaTinhDen nchar(20) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'GioKhoiHanh') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD GioKhoiHanh time(0) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'NgayKetThuc') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD NgayKetThuc date NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'GioKetThuc') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD GioKetThuc time(0) NULL;
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'SoSuKienMoiNgay') IS NULL
    ALTER TABLE dbo.YeuCauThietKe ADD SoSuKienMoiNgay int NULL;
GO

IF COL_LENGTH(N'dbo.LichTrinh', N'GioBatDau') IS NULL
    ALTER TABLE dbo.LichTrinh ADD GioBatDau time(0) NULL;
GO

IF OBJECT_ID(N'dbo.TinhThanhAlias', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinhThanhAlias
    (
        MaAlias int IDENTITY(1,1) NOT NULL,
        MaTinh nchar(20) NOT NULL,
        TenAlias nvarchar(100) NOT NULL,
        CONSTRAINT PK_TinhThanhAlias PRIMARY KEY (MaAlias),
        CONSTRAINT FK_TinhThanhAlias_Tinh FOREIGN KEY (MaTinh) REFERENCES dbo.TinhThanh(MaTinh),
        CONSTRAINT UQ_TinhThanhAlias UNIQUE (TenAlias)
    );
END;
GO

IF OBJECT_ID(N'dbo.MatranDiChuyen', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MatranDiChuyen
    (
        MaTinhDi nchar(20) NOT NULL,
        MaTinhDen nchar(20) NOT NULL,
        PhuongTien nchar(20) NOT NULL,
        SoPhut int NOT NULL,
        ChiPhiUocTinh int NULL,
        CONSTRAINT PK_MatranDiChuyen PRIMARY KEY (MaTinhDi, MaTinhDen, PhuongTien),
        CONSTRAINT FK_Matran_Di FOREIGN KEY (MaTinhDi) REFERENCES dbo.TinhThanh(MaTinh),
        CONSTRAINT FK_Matran_Den FOREIGN KEY (MaTinhDen) REFERENCES dbo.TinhThanh(MaTinh),
        CONSTRAINT CK_Matran_PhuongTien CHECK (PhuongTien IN (N'MayBay', N'Tau', N'XeKhach', N'XeMay')),
        CONSTRAINT CK_Matran_SoPhut CHECK (SoPhut >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.HoiThoaiThietKe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoiThoaiThietKe
    (
        MaHoiThoai nchar(20) NOT NULL,
        MaUser nchar(20) NOT NULL,
        MaYeuCau nchar(20) NULL,
        TrangThai nchar(20) NOT NULL CONSTRAINT DF_HoiThoai_TrangThai DEFAULT N'DangHoi',
        DuLieuJson nvarchar(max) NULL,
        NgayTao datetime2 NOT NULL CONSTRAINT DF_HoiThoai_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_HoiThoaiThietKe PRIMARY KEY (MaHoiThoai),
        CONSTRAINT FK_HoiThoai_User FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser),
        CONSTRAINT FK_HoiThoai_YeuCau FOREIGN KEY (MaYeuCau) REFERENCES dbo.YeuCauThietKe(MaYeuCau),
        CONSTRAINT CK_HoiThoai_TrangThai CHECK (TrangThai IN (N'DangHoi', N'DaSinhDeXuat', N'Huy'))
    );
END;
GO

IF OBJECT_ID(N'dbo.TinNhanThietKe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinNhanThietKe
    (
        MaTinNhan nchar(20) NOT NULL,
        MaHoiThoai nchar(20) NOT NULL,
        VaiTro nchar(20) NOT NULL,
        NoiDung nvarchar(max) NOT NULL,
        PayloadJson nvarchar(max) NULL,
        NgayTao datetime2 NOT NULL CONSTRAINT DF_TinNhan_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TinNhanThietKe PRIMARY KEY (MaTinNhan),
        CONSTRAINT FK_TinNhan_HoiThoai FOREIGN KEY (MaHoiThoai) REFERENCES dbo.HoiThoaiThietKe(MaHoiThoai) ON DELETE CASCADE,
        CONSTRAINT CK_TinNhan_VaiTro CHECK (VaiTro IN (N'Bot', N'Khach'))
    );
END;
GO

IF OBJECT_ID(N'dbo.FK_YeuCau_TinhXuatPhat', N'F') IS NULL
    ALTER TABLE dbo.YeuCauThietKe WITH NOCHECK
    ADD CONSTRAINT FK_YeuCau_TinhXuatPhat FOREIGN KEY (MaTinhXuatPhat) REFERENCES dbo.TinhThanh(MaTinh);
IF OBJECT_ID(N'dbo.FK_YeuCau_TinhDen', N'F') IS NULL
    ALTER TABLE dbo.YeuCauThietKe WITH NOCHECK
    ADD CONSTRAINT FK_YeuCau_TinhDen FOREIGN KEY (MaTinhDen) REFERENCES dbo.TinhThanh(MaTinh);
GO

-- Alias thành phố / tên hay gõ (không phân biệt hoa thường khi resolve ở C#)
MERGE dbo.TinhThanhAlias AS t
USING (VALUES
    (N'TN01                ', N'Hà Nội'),
    (N'TN01                ', N'Ha Noi'),
    (N'TN01                ', N'Hanoi'),
    (N'TN01                ', N'Thủ đô'),
    (N'TN03                ', N'Hạ Long'),
    (N'TN03                ', N'Ha Long'),
    (N'TN03                ', N'Vịnh Hạ Long'),
    (N'TN15                ', N'Sa Pa'),
    (N'TN15                ', N'Sapa'),
    (N'TN32                ', N'Đà Nẵng'),
    (N'TN32                ', N'Da Nang'),
    (N'TN33                ', N'Hội An'),
    (N'TN33                ', N'Hoi An'),
    (N'TN31                ', N'Huế'),
    (N'TN31                ', N'Hue'),
    (N'TN31                ', N'Thừa Thiên Huế'),
    (N'TN37                ', N'Nha Trang'),
    (N'TN37                ', N'Nhatrang'),
    (N'TN37                ', N'Khánh Hòa'),
    (N'TN45                ', N'Sài Gòn'),
    (N'TN45                ', N'Sai Gon'),
    (N'TN45                ', N'TP.HCM'),
    (N'TN45                ', N'TPHCM'),
    (N'TN45                ', N'Hồ Chí Minh'),
    (N'TN45                ', N'Ho Chi Minh'),
    (N'TN44                ', N'Đà Lạt'),
    (N'TN44                ', N'Da Lat'),
    (N'TN44                ', N'Dalat'),
    (N'TN58                ', N'Phú Quốc'),
    (N'TN58                ', N'Phu Quoc'),
    (N'TN48                ', N'Vũng Tàu'),
    (N'TN48                ', N'Vung Tau')
) AS s(MaTinh, TenAlias)
ON t.TenAlias = s.TenAlias
WHEN NOT MATCHED THEN INSERT (MaTinh, TenAlias) VALUES (s.MaTinh, s.TenAlias);
GO

-- Backfill MaTinh cho điểm/đối tác seed cũ (014) — đây là gốc lỗi Nha Trang → Hà Nội
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN37                '
 WHERE (TenDiaDanh LIKE N'%Nha Trang%' OR TenDiaDanh LIKE N'%VinWonders Nha Trang%')
   AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN03                '
 WHERE (TenDiaDanh LIKE N'%Hạ Long%' OR TenDiaDanh LIKE N'%Ha Long%')
   AND (MaTinh IS NULL OR MaTinh = N'TN01                ');
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN15                '
 WHERE TenDiaDanh LIKE N'%Sa Pa%' AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN33                '
 WHERE TenDiaDanh LIKE N'%Hội An%' AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN58                '
 WHERE TenDiaDanh LIKE N'%Phú Quốc%' AND MaTinh IS NULL;
GO

-- Ma trận phút cho các cặp hay demo. Cặp khác: C# ước theo miền.
-- PhuongTien padded by C# PadTo20; store trimmed then pad here.
DECLARE @pairs TABLE (Di nchar(20), Den nchar(20), Bay int, Tau int, Xe int, May int, GiaBay int, GiaXe int);
INSERT INTO @pairs VALUES
 (N'TN01                ', N'TN37                ', 210, 720, 720, 840, 1800000, 450000), -- HN ↔ Nha Trang
 (N'TN01                ', N'TN32                ', 195, 960, 960, 1080, 1600000, 500000), -- HN ↔ Đà Nẵng
 (N'TN01                ', N'TN45                ', 240, 1800, 2100, 2400, 2200000, 800000), -- HN ↔ HCM
 (N'TN01                ', N'TN03                ',  90, 240, 240, 270,  0, 180000), -- HN ↔ Hạ Long
 (N'TN01                ', N'TN15                ', 210, 480, 420, 480,  0, 350000), -- HN ↔ Sapa
 (N'TN01                ', N'TN31                ', 195, 780, 780, 900, 1500000, 480000), -- HN ↔ Huế
 (N'TN32                ', N'TN37                ',  75, 300, 300, 360,  900000, 220000), -- ĐN ↔ Nha Trang
 (N'TN32                ', N'TN33                ',  50,  60,  50,  70,  0, 80000), -- ĐN ↔ Hội An
 (N'TN32                ', N'TN45                ', 120, 1200, 1080, 1260, 1400000, 550000),
 (N'TN45                ', N'TN37                ',  75, 480, 480, 540, 1200000, 280000), -- HCM ↔ Nha Trang
 (N'TN45                ', N'TN58                ',  75, 0, 0, 0, 1500000, 0), -- HCM ↔ Phú Quốc (máy bay)
 (N'TN45                ', N'TN44                ',  60, 420, 420, 480,  900000, 250000); -- HCM ↔ Đà Lạt

INSERT INTO dbo.MatranDiChuyen (MaTinhDi, MaTinhDen, PhuongTien, SoPhut, ChiPhiUocTinh)
SELECT p.Di, p.Den, LEFT(N'MayBay'+REPLICATE(N' ',20),20), p.Bay, p.GiaBay FROM @pairs p
WHERE p.Bay > 0 AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Di AND m.MaTinhDen=p.Den AND m.PhuongTien LIKE N'MayBay%')
UNION ALL
SELECT p.Den, p.Di, LEFT(N'MayBay'+REPLICATE(N' ',20),20), p.Bay, p.GiaBay FROM @pairs p
WHERE p.Bay > 0 AND p.Di <> p.Den AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Den AND m.MaTinhDen=p.Di AND m.PhuongTien LIKE N'MayBay%')
UNION ALL
SELECT p.Di, p.Den, LEFT(N'Tau'+REPLICATE(N' ',20),20), p.Tau, NULL FROM @pairs p
WHERE p.Tau > 0 AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Di AND m.MaTinhDen=p.Den AND m.PhuongTien LIKE N'Tau%')
UNION ALL
SELECT p.Den, p.Di, LEFT(N'Tau'+REPLICATE(N' ',20),20), p.Tau, NULL FROM @pairs p
WHERE p.Tau > 0 AND p.Di <> p.Den AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Den AND m.MaTinhDen=p.Di AND m.PhuongTien LIKE N'Tau%')
UNION ALL
SELECT p.Di, p.Den, LEFT(N'XeKhach'+REPLICATE(N' ',20),20), p.Xe, p.GiaXe FROM @pairs p
WHERE p.Xe > 0 AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Di AND m.MaTinhDen=p.Den AND m.PhuongTien LIKE N'XeKhach%')
UNION ALL
SELECT p.Den, p.Di, LEFT(N'XeKhach'+REPLICATE(N' ',20),20), p.Xe, p.GiaXe FROM @pairs p
WHERE p.Xe > 0 AND p.Di <> p.Den AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Den AND m.MaTinhDen=p.Di AND m.PhuongTien LIKE N'XeKhach%')
UNION ALL
SELECT p.Di, p.Den, LEFT(N'XeMay'+REPLICATE(N' ',20),20), p.May, NULL FROM @pairs p
WHERE p.May > 0 AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Di AND m.MaTinhDen=p.Den AND m.PhuongTien LIKE N'XeMay%')
UNION ALL
SELECT p.Den, p.Di, LEFT(N'XeMay'+REPLICATE(N' ',20),20), p.May, NULL FROM @pairs p
WHERE p.May > 0 AND p.Di <> p.Den AND NOT EXISTS (SELECT 1 FROM dbo.MatranDiChuyen m WHERE m.MaTinhDi=p.Den AND m.MaTinhDen=p.Di AND m.PhuongTien LIKE N'XeMay%');
GO
