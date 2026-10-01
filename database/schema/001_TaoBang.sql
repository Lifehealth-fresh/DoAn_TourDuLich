/* 001 — Tạo bảng, cột, ràng buộc, chỉ mục.
   Chỉ những bảng sản phẩm còn dùng.
   Không tạo (và không cần DROP) các bảng đã bỏ:
   HuongDanVien, LichDanTour, DanhGiaHDV, DanhGiaSanPhamDoiTac,
   ThongBao, Quyen, MediaDanhGiaHdv, MediaDanhGiaSanPham,
   JobRunLog, HoiThoaiThietKe, TinNhanThietKe.
   Cột Tour.SLHuongDanVien vẫn giữ vì schema nghiệp vụ còn cột này.
   Chạy trên database TourDuLich trống. Không có USE.
*/

/* ===== 001_CreateSchema.sql #1 ===== */
/* Chạy trong database Azure SQL trống hoàn toàn; không chuyển ngữ cảnh database. */

CREATE TABLE dbo.KhuVuc
(
    MaKhuVuc nchar(20) NOT NULL CONSTRAINT PK_KhuVuc PRIMARY KEY,
    TenKhuVuc nvarchar(50) NULL,
    QuocGia nchar(20) NULL,
    ViDo decimal(9,6) NULL,
    KinhDo decimal(9,6) NULL,
    MuiGio nvarchar(30) NULL,
    TrangThai nchar(20) NULL
);

CREATE TABLE dbo.VaiTro
(
    MaVaiTro int NOT NULL CONSTRAINT PK_VaiTro PRIMARY KEY,
    TenVaiTro nvarchar(50) NOT NULL,
    Mota nvarchar(100) NULL
);

CREATE TABLE dbo.NhomKhuyenMai
(
    MaNhomKM nchar(20) NOT NULL CONSTRAINT PK_NhomKhuyenMai PRIMARY KEY,
    TenNhomKM nvarchar(50) NULL
);

CREATE TABLE dbo.Tour
(
    MaTour nchar(20) NOT NULL CONSTRAINT PK_Tour PRIMARY KEY,
    TenTour nvarchar(150) NOT NULL,
    Mota nvarchar(max) NULL,
    ThoiGian int NULL,
    DieuKhoan nvarchar(max) NULL,
    GiaTour int NOT NULL,
    SLKhach int NOT NULL,
    SLHuongDanVien int NULL,
    LoaiTour nchar(20) NOT NULL CONSTRAINT DF_Tour_LoaiTour DEFAULT N'Chuan',
    TrangThai nchar(20) NULL,
    CONSTRAINT CK_Tour_LoaiTour CHECK (RTRIM(LoaiTour) IN (N'Chuan', N'TuThietKe'))
);


CREATE TABLE dbo.NguoiSuDung
(
    MaUser nchar(20) NOT NULL CONSTRAINT PK_NguoiSuDung PRIMARY KEY,
    SoDienThoai nchar(20) NOT NULL,
    MatKhau nvarchar(200) NOT NULL,
    MaVaiTro int NOT NULL,
    CONSTRAINT UQ_NguoiSuDung_SoDienThoai UNIQUE (SoDienThoai),
    CONSTRAINT FK_NguoiSuDung_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES dbo.VaiTro(MaVaiTro)
);

CREATE TABLE dbo.DiemThamQuan
(
    MaDThamQuan nchar(20) NOT NULL CONSTRAINT PK_DiemThamQuan PRIMARY KEY,
    TenDiaDanh nvarchar(100) NULL,
    DiaChi nchar(100) NULL,
    MaKhuVuc nchar(20) NULL,
    KinhDo decimal(9,6) NULL,
    ViDo decimal(9,6) NULL,
    Mota nvarchar(max) NULL,
    CONSTRAINT FK_DiemThamQuan_KhuVuc FOREIGN KEY (MaKhuVuc) REFERENCES dbo.KhuVuc(MaKhuVuc)
);

CREATE TABLE dbo.DoiTac
(
    MaDoiTac nchar(20) NOT NULL CONSTRAINT PK_DoiTac PRIMARY KEY,
    TenDoiTac nvarchar(150) NOT NULL,
    LoaiDoiTac nchar(20) NOT NULL,
    NguoiLienHe nvarchar(50) NULL,
    SoDienThoai nchar(20) NULL,
    Email nvarchar(100) NULL,
    MaKhuVuc nchar(20) NULL,
    PhanTramHoaHong decimal(5,2) NULL,
    TrangThai nchar(20) NULL,
    CONSTRAINT CK_DoiTac_LoaiDoiTac CHECK (RTRIM(LoaiDoiTac) IN (N'LuuTru', N'VanChuyen', N'AnUong', N'HoatDong')),
    CONSTRAINT FK_DoiTac_KhuVuc FOREIGN KEY (MaKhuVuc) REFERENCES dbo.KhuVuc(MaKhuVuc)
);

CREATE TABLE dbo.SanPhamDoiTac
(
    MaSanPham nchar(20) NOT NULL CONSTRAINT PK_SanPhamDoiTac PRIMARY KEY,
    MaDoiTac nchar(20) NOT NULL,
    TenSanPham nvarchar(150) NOT NULL,
    DonViTinh nvarchar(30) NULL,
    GiaNiemYet int NOT NULL,
    MaDThamQuan nchar(20) NULL,
    Mota nvarchar(300) NULL,
    TrangThai nchar(20) NULL,
    CONSTRAINT FK_SanPhamDoiTac_DoiTac FOREIGN KEY (MaDoiTac) REFERENCES dbo.DoiTac(MaDoiTac),
    CONSTRAINT FK_SanPhamDoiTac_DiemThamQuan FOREIGN KEY (MaDThamQuan) REFERENCES dbo.DiemThamQuan(MaDThamQuan)
);

CREATE TABLE dbo.AIGoiY
(
    MaRecommodation nchar(20) NOT NULL CONSTRAINT PK_AIGoiY PRIMARY KEY,
    MaUser nchar(20) NULL,
    MaTour nchar(20) NULL,
    DiemPhuHop float NULL,
    LyDo nvarchar(200) NULL,
    NgayGoiY datetime NULL,
    CONSTRAINT FK_AIGoiY_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_AIGoiY_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.AnhTour
(
    MaAnhTour nchar(20) NOT NULL CONSTRAINT PK_AnhTour PRIMARY KEY,
    MaTour nchar(20) NOT NULL,
    ImageURL nvarchar(300) NULL,
    ThuTu int NULL,
    IsAvatar bit NULL,
    CONSTRAINT FK_AnhTour_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour)
);

CREATE TABLE dbo.LichKhoiHanh
(
    MaKhoiHanh nchar(20) NOT NULL CONSTRAINT PK_LichKhoiHanh PRIMARY KEY,
    MaTour nchar(20) NOT NULL,
    NgayKhoiHanh datetime NULL,
    NgayKetThuc datetime NULL,
    DiaDiem nvarchar(100) NULL,
    CONSTRAINT FK_LichKhoiHanh_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour)
);

CREATE TABLE dbo.KhuyenMai
(
    MaKM nchar(20) NOT NULL CONSTRAINT PK_KhuyenMai PRIMARY KEY,
    MaNhomKM nchar(20) NULL,
    TenKM nvarchar(50) NULL,
    MaCode nchar(10) NULL,
    NgayBD datetime NULL,
    NgayKT datetime NULL,
    DonVi nchar(20) NULL,
    GiamGia int NULL,
    CoCongDon bit NULL,
    TrangThai nchar(20) NULL,
    CONSTRAINT FK_KhuyenMai_NhomKM FOREIGN KEY (MaNhomKM) REFERENCES dbo.NhomKhuyenMai(MaNhomKM)
);

CREATE TABLE dbo.DieuKienKM
(
    MaDK nchar(20) NOT NULL CONSTRAINT PK_DieuKienKM PRIMARY KEY,
    MaKhuyenMai nchar(20) NULL,
    DonToiThieu int NULL,
    LanDatDau bit NULL,
    SoLuong int NULL,
    CONSTRAINT FK_DieuKienKM_KhuyenMai FOREIGN KEY (MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKM)
);

CREATE TABLE dbo.KM_Tour
(
    STT int NOT NULL CONSTRAINT PK_KM_Tour PRIMARY KEY,
    MaKhuyenMai nchar(20) NOT NULL,
    MaTour nchar(20) NOT NULL,
    CONSTRAINT FK_KMTour_KhuyenMai FOREIGN KEY (MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKM),
    CONSTRAINT FK_KMTour_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour)
);

CREATE TABLE dbo.DatDichVu
(
    MaBooking nchar(20) NOT NULL CONSTRAINT PK_DatDichVu PRIMARY KEY,
    MaUser nchar(20) NOT NULL,
    MaTour nchar(20) NOT NULL,
    MaKhoiHanh nchar(20) NULL,
    NgayDat date NULL,
    SLNguoiLon int NULL,
    SLTreEm int NULL,
    TongTien int NULL,
    TongGiamGia int NULL,
    ThanhTien int NULL,
    TrangThai nchar(20) NULL,
    CONSTRAINT FK_DatDichVu_KhoiHanh FOREIGN KEY (MaKhoiHanh) REFERENCES dbo.LichKhoiHanh(MaKhoiHanh),
    CONSTRAINT FK_DatDichVu_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_DatDichVu_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.DatDichVu_KhuyenMai
(
    STT int NOT NULL CONSTRAINT PK_DatDichVu_KhuyenMai PRIMARY KEY,
    MaBooking nchar(20) NOT NULL,
    MaKhuyenMai nchar(20) NOT NULL,
    SoTienGiam int NULL,
    CONSTRAINT FK_DDVKM_DatDichVu FOREIGN KEY (MaBooking) REFERENCES dbo.DatDichVu(MaBooking),
    CONSTRAINT FK_DDVKM_KhuyenMai FOREIGN KEY (MaKhuyenMai) REFERENCES dbo.KhuyenMai(MaKM)
);

CREATE TABLE dbo.ThanhToan
(
    MaTT nchar(20) NOT NULL CONSTRAINT PK_ThanhToan PRIMARY KEY,
    MaBooking nchar(20) NOT NULL,
    SoTien int NULL,
    NgayTT datetime NULL,
    TrangThai nchar(20) NULL,
    PhuongThuc nvarchar(30) NULL,
    LoaiThanhToan nchar(20) NULL,
    IdempotencyKey nvarchar(100) NULL,
    CONSTRAINT FK_ThanhToan_DatDichVu FOREIGN KEY (MaBooking) REFERENCES dbo.DatDichVu(MaBooking)
);

CREATE TABLE dbo.HopDong
(
    MaHopDong nchar(20) NOT NULL CONSTRAINT PK_HopDong PRIMARY KEY,
    MaBooking nchar(20) NOT NULL,
    SoHopDong nvarchar(50) NULL,
    NgayKy date NULL,
    DieuKhoanCamKet nvarchar(max) NULL,
    FileHopDongURL nvarchar(300) NULL,
    NguoiDaiDien nchar(20) NULL,
    TrangThai nchar(20) NULL,
    CONSTRAINT FK_HopDong_DatDichVu FOREIGN KEY (MaBooking) REFERENCES dbo.DatDichVu(MaBooking),
    CONSTRAINT FK_HopDong_NguoiSuDung FOREIGN KEY (NguoiDaiDien) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.KhachHang
(
    MaKhachHang nchar(20) NOT NULL CONSTRAINT PK_KhachHang PRIMARY KEY,
    Ho nvarchar(20) NOT NULL,
    Ten nvarchar(50) NOT NULL,
    HoGiayTo nvarchar(20) NULL,
    TenGiayTo nvarchar(50) NULL,
    QuocTich nvarchar(50) NULL,
    DanhXung nchar(10) NULL,
    GioiTinh nchar(10) NULL,
    NgaySinh date NULL,
    Email nvarchar(100) NULL,
    SoDienThoai nchar(15) NOT NULL,
    MaUser nchar(20) NOT NULL,
    CONSTRAINT FK_KhachHang_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.GiayTo
(
    MaGiayTo nchar(20) NOT NULL CONSTRAINT PK_GiayTo PRIMARY KEY,
    LoaiGiayTo nvarchar(50) NOT NULL,
    SoTrenGiayTo nvarchar(50) NOT NULL,
    NgayCap date NOT NULL,
    NgayHetHan date NOT NULL,
    NoiCap nvarchar(50) NOT NULL,
    MaKhachHang nchar(20) NOT NULL,
    CONSTRAINT FK_GiayTo_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES dbo.KhachHang(MaKhachHang)
);

CREATE TABLE dbo.LichTrinh
(
    MaLichTrinh nchar(20) NOT NULL CONSTRAINT PK_LichTrinh PRIMARY KEY,
    MaTour nchar(20) NOT NULL,
    NgayThu int NULL,
    ThuTuTrongNgay int NULL,
    MaDThamQuan nchar(20) NULL,
    MaSanPham nchar(20) NULL,
    SoLuong int NULL,
    DonGia int NULL,
    ThanhTien AS (ISNULL(SoLuong, 0) * ISNULL(DonGia, 0)) PERSISTED,
    ThoiGianDuKien datetime NULL,
    Mota nvarchar(max) NULL,
    CONSTRAINT FK_LichTrinh_DiemThamQuan FOREIGN KEY (MaDThamQuan) REFERENCES dbo.DiemThamQuan(MaDThamQuan),
    CONSTRAINT FK_LichTrinh_SanPham FOREIGN KEY (MaSanPham) REFERENCES dbo.SanPhamDoiTac(MaSanPham),
    CONSTRAINT FK_LichTrinh_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour)
);


CREATE TABLE dbo.DanhGiaTour
(
    MaDanhGiaTour nchar(20) NOT NULL CONSTRAINT PK_DanhGiaTour PRIMARY KEY,
    MaUser nchar(20) NULL,
    MaTour nchar(20) NULL,
    ThoiGian datetime2(7) NULL,
    SaoDanhGia int NULL,
    NhanXet nvarchar(max) NULL,
    CONSTRAINT FK_DanhGiaTour_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_DanhGiaTour_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);


CREATE TABLE dbo.DanhSachYeuThich
(
    MaWish nchar(20) NOT NULL CONSTRAINT PK_DanhSachYeuThich PRIMARY KEY,
    MaUser nchar(20) NULL,
    MaTour nchar(20) NULL,
    NgayThem date NULL,
    CONSTRAINT FK_YeuThich_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_YeuThich_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.HanhViKhachHang
(
    MaHanhDong nchar(20) NOT NULL CONSTRAINT PK_HanhViKhachHang PRIMARY KEY,
    MaUser nchar(20) NULL,
    MaTour nchar(20) NULL,
    HanhDong nvarchar(20) NULL,
    ThoiGian datetime NULL,
    CONSTRAINT FK_HanhVi_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_HanhVi_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);


CREATE TABLE dbo.YeuCauThietKe
(
    MaYeuCau nchar(20) NOT NULL CONSTRAINT PK_YeuCauThietKe PRIMARY KEY,
    MaUser nchar(20) NOT NULL,
    DiemDenMongMuon nvarchar(200) NULL,
    NgayDuKienDi date NULL,
    SoNgay int NULL,
    SoNguoiLon int NULL,
    SoTreEm int NULL,
    NganSachDuKien int NULL,
    SoThichGhiChu nvarchar(max) NULL,
    MaGoiYThamKhao nchar(20) NULL,
    LyDoTuChoiGoiY nvarchar(200) NULL,
    TrangThai nchar(20) NULL,
    NgayGui datetime NULL,
    MaTourTao nchar(20) NULL,
    CONSTRAINT FK_YeuCauThietKe_AIGoiY FOREIGN KEY (MaGoiYThamKhao) REFERENCES dbo.AIGoiY(MaRecommodation),
    CONSTRAINT FK_YeuCauThietKe_Tour FOREIGN KEY (MaTourTao) REFERENCES dbo.Tour(MaTour),
    CONSTRAINT FK_YeuCauThietKe_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);
GO

/* ===== 004_Indexes.sql #1 ===== */
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
-- Enforce one review per customer and reviewed object. Application-level checks
-- provide a friendly 409; these unique indexes close the concurrent-request race.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DanhGiaTour_MaUser_MaTour' AND object_id = OBJECT_ID(N'dbo.DanhGiaTour')) CREATE UNIQUE INDEX UX_DanhGiaTour_MaUser_MaTour ON dbo.DanhGiaTour(MaUser, MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThietKe_MaUser' AND object_id = OBJECT_ID(N'dbo.YeuCauThietKe')) CREATE INDEX IX_YeuCauThietKe_MaUser ON dbo.YeuCauThietKe(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThietKe_MaTourTao' AND object_id = OBJECT_ID(N'dbo.YeuCauThietKe')) CREATE INDEX IX_YeuCauThietKe_MaTourTao ON dbo.YeuCauThietKe(MaTourTao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichTrinh_MaTour' AND object_id = OBJECT_ID(N'dbo.LichTrinh')) CREATE INDEX IX_LichTrinh_MaTour ON dbo.LichTrinh(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichKhoiHanh_MaTour' AND object_id = OBJECT_ID(N'dbo.LichKhoiHanh')) CREATE INDEX IX_LichKhoiHanh_MaTour ON dbo.LichKhoiHanh(MaTour);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIGoiY_MaUser' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE INDEX IX_AIGoiY_MaUser ON dbo.AIGoiY(MaUser);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIGoiY_MaUser_NgayGoiY' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE INDEX IX_AIGoiY_MaUser_NgayGoiY ON dbo.AIGoiY(MaUser, NgayGoiY DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatDichVu_MaUser_NgayDat' AND object_id = OBJECT_ID(N'dbo.DatDichVu')) CREATE INDEX IX_DatDichVu_MaUser_NgayDat ON dbo.DatDichVu(MaUser, NgayDat DESC);
-- One current recommendation per user/tour. ASP.NET replaces a user's set in
-- one transaction; this index also rejects concurrent or malformed duplicates.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AIGoiY_MaUser_MaTour' AND object_id = OBJECT_ID(N'dbo.AIGoiY')) CREATE UNIQUE INDEX UX_AIGoiY_MaUser_MaTour ON dbo.AIGoiY(MaUser, MaTour);
GO

/* ===== 006_HopDong_TuDongDien.sql #1 ===== */
/* Chạy thủ công trên database đã có schema 001-005. Không tự chạy migration. */
ALTER TABLE dbo.DatDichVu ADD MaKhachHang nchar(20) NULL;
GO

/* ===== 006_HopDong_TuDongDien.sql #2 ===== */
ALTER TABLE dbo.DatDichVu
ADD CONSTRAINT FK_DatDichVu_KhachHang FOREIGN KEY (MaKhachHang)
REFERENCES dbo.KhachHang (MaKhachHang);
GO

/* ===== 006_HopDong_TuDongDien.sql #3 ===== */
ALTER TABLE dbo.HopDong ADD HoTenKhach nvarchar(70) NULL;
GO

/* ===== 006_HopDong_TuDongDien.sql #4 ===== */
ALTER TABLE dbo.HopDong ADD LoaiGiayTo nvarchar(50) NULL;
GO

/* ===== 006_HopDong_TuDongDien.sql #5 ===== */
ALTER TABLE dbo.HopDong ADD SoGiayTo nvarchar(50) NULL;
GO

/* ===== 006_HopDong_TuDongDien.sql #6 ===== */
ALTER TABLE dbo.DatDichVu ADD TyLePhatHuy INT NULL;
GO

/* ===== 006_HopDong_TuDongDien.sql #7 ===== */
ALTER TABLE dbo.DatDichVu ADD SoTienPhatHuy INT NULL;
GO

/* ===== 007_LichTrinhDeXuat.sql #1 ===== */
IF COL_LENGTH(N'dbo.YeuCauThietKe', N'LyDoTuChoiBoiSale') IS NULL
BEGIN
    ALTER TABLE dbo.YeuCauThietKe ADD LyDoTuChoiBoiSale nvarchar(max) NULL;
END;
GO

/* ===== 007_LichTrinhDeXuat.sql #2 ===== */
IF OBJECT_ID(N'dbo.LichTrinhDeXuat', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichTrinhDeXuat
    (
        MaDeXuat nchar(20) NOT NULL,
        MaYeuCau nchar(20) NOT NULL,
        ThuTuPhuongAn int NOT NULL,
        TenPhuongAn nvarchar(100) NOT NULL,
        TongTienDuKien int NOT NULL,
        GhiChu nvarchar(500) NULL,
        TrangThai nchar(20) NOT NULL CONSTRAINT DF_LichTrinhDeXuat_TrangThai DEFAULT N'DeXuat',
        NgayTao datetime2 NOT NULL CONSTRAINT DF_LichTrinhDeXuat_NgayTao DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_LichTrinhDeXuat PRIMARY KEY (MaDeXuat),
        CONSTRAINT FK_LichTrinhDeXuat_YeuCau FOREIGN KEY (MaYeuCau)
            REFERENCES dbo.YeuCauThietKe(MaYeuCau) ON DELETE CASCADE,
        CONSTRAINT CK_LichTrinhDeXuat_TrangThai CHECK (TrangThai IN (N'DeXuat', N'DaChon', N'KhongChon'))
    );
END;
GO

/* ===== 007_LichTrinhDeXuat.sql #3 ===== */
IF OBJECT_ID(N'dbo.LichTrinhDeXuatChiTiet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichTrinhDeXuatChiTiet
    (
        MaChiTiet nchar(20) NOT NULL,
        MaDeXuat nchar(20) NOT NULL,
        NgayThu int NOT NULL,
        ThuTuTrongNgay int NOT NULL,
        MaDThamQuan nchar(20) NULL,
        MaSanPham nchar(20) NULL,
        SoLuong int NOT NULL,
        DonGia int NOT NULL,
        ThanhTien int NOT NULL,
        Mota nvarchar(500) NULL,
        CONSTRAINT PK_LichTrinhDeXuatChiTiet PRIMARY KEY (MaChiTiet),
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_DeXuat FOREIGN KEY (MaDeXuat)
            REFERENCES dbo.LichTrinhDeXuat(MaDeXuat) ON DELETE CASCADE,
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_Diem FOREIGN KEY (MaDThamQuan)
            REFERENCES dbo.DiemThamQuan(MaDThamQuan),
        CONSTRAINT FK_LichTrinhDeXuatChiTiet_SanPham FOREIGN KEY (MaSanPham)
            REFERENCES dbo.SanPhamDoiTac(MaSanPham)
    );
END;
GO

/* ===== 008_AnhTour_Video.sql #1 ===== */
-- Adds media type and a provider-neutral URL column while retaining ImageURL
-- for backward compatibility with the original schema and existing clients.
-- Dynamic batches are intentional: SQL Server binds column names before executing
-- ALTER TABLE in the same batch.
IF COL_LENGTH(N'dbo.AnhTour', N'LoaiMedia') IS NULL
BEGIN
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ADD LoaiMedia nvarchar(10) NULL
        CONSTRAINT DF_AnhTour_LoaiMedia DEFAULT N''Anh'';';
    EXEC sys.sp_executesql N'UPDATE dbo.AnhTour SET LoaiMedia = N''Anh'' WHERE LoaiMedia IS NULL;';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ALTER COLUMN LoaiMedia nvarchar(10) NOT NULL;';
END;
GO

/* ===== 008_AnhTour_Video.sql #2 ===== */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AnhTour_LoaiMedia')
BEGIN
    EXEC sys.sp_executesql N'
        ALTER TABLE dbo.AnhTour ADD CONSTRAINT CK_AnhTour_LoaiMedia
            CHECK (RTRIM(LoaiMedia) IN (N''Anh'', N''Video''));';
END;
GO

/* ===== 008_AnhTour_Video.sql #3 ===== */
IF COL_LENGTH(N'dbo.AnhTour', N'Url') IS NULL
BEGIN
    EXEC sys.sp_executesql N'ALTER TABLE dbo.AnhTour ADD Url nvarchar(300) NULL;';
    EXEC sys.sp_executesql N'UPDATE dbo.AnhTour SET Url = ImageURL WHERE Url IS NULL;';
END;
GO

/* ===== 009_MediaDanhGia.sql #1 ===== */
IF OBJECT_ID(N'dbo.MediaDanhGiaTour', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaDanhGiaTour
    (
        MaMedia nchar(20) NOT NULL,
        MaDanhGiaTour nchar(20) NOT NULL,
        LoaiMedia nvarchar(10) NOT NULL,
        Url nvarchar(300) NOT NULL,
        ThuTu int NULL,
        CONSTRAINT PK_MediaDanhGiaTour PRIMARY KEY (MaMedia),
        CONSTRAINT FK_MediaDanhGiaTour_DanhGiaTour FOREIGN KEY (MaDanhGiaTour)
            REFERENCES dbo.DanhGiaTour(MaDanhGiaTour) ON DELETE CASCADE,
        CONSTRAINT CK_MediaDanhGiaTour_LoaiMedia CHECK (LoaiMedia IN (N'Anh', N'Video'))
    );
END;
GO

/* ===== 009_MediaDanhGia.sql #2 ===== */
GO

/* ===== 009_MediaDanhGia.sql #3 ===== */
GO

/* ===== 010_ThanhToan_Idempotency.sql #1 ===== */
/* Idempotency key for safe payment retries. Run after 001-009 on existing DBs. */
IF COL_LENGTH(N'dbo.ThanhToan', N'IdempotencyKey') IS NULL
    ALTER TABLE dbo.ThanhToan ADD IdempotencyKey nvarchar(100) NULL;
GO

/* ===== 010_ThanhToan_Idempotency.sql #2 ===== */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ThanhToan_MaBooking_IdempotencyKey'
               AND object_id = OBJECT_ID(N'dbo.ThanhToan'))
    CREATE UNIQUE INDEX UX_ThanhToan_MaBooking_IdempotencyKey
        ON dbo.ThanhToan(MaBooking, IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
GO

/* ===== 011_JobRunLog.sql #1 ===== */
/* Observability for scheduled data jobs. Safe to run on an existing database. */
GO
GO

/* ===== 012_AnhTour_Cloudinary.sql #1 ===== */
/* Metadata required to manage public Cloudinary assets after upload. */
IF COL_LENGTH(N'dbo.AnhTour', N'CloudPublicId') IS NULL
    ALTER TABLE dbo.AnhTour ADD CloudPublicId nvarchar(255) NULL;
GO

/* ===== 012_AnhTour_Cloudinary.sql #2 ===== */
IF COL_LENGTH(N'dbo.AnhTour', N'CloudResourceType') IS NULL
    ALTER TABLE dbo.AnhTour ADD CloudResourceType nvarchar(10) NULL;
GO

/* ===== 012_AnhTour_Cloudinary.sql #3 ===== */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AnhTour_CloudPublicId' AND object_id = OBJECT_ID(N'dbo.AnhTour'))
    CREATE INDEX IX_AnhTour_CloudPublicId ON dbo.AnhTour(CloudPublicId) WHERE CloudPublicId IS NOT NULL;
GO

/* ===== 015_ThanhToan_PaymentGateway.sql #1 ===== */
/* Payment gateway metadata. Run after 001-014 on existing databases. */
IF COL_LENGTH(N'dbo.ThanhToan', N'Gateway') IS NULL
    ALTER TABLE dbo.ThanhToan ADD Gateway nvarchar(20) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'GatewayTxnId') IS NULL
    ALTER TABLE dbo.ThanhToan ADD GatewayTxnId nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'GatewayOrderId') IS NULL
    ALTER TABLE dbo.ThanhToan ADD GatewayOrderId nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'PayUrl') IS NULL
    ALTER TABLE dbo.ThanhToan ADD PayUrl nvarchar(2000) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'PaidAt') IS NULL
    ALTER TABLE dbo.ThanhToan ADD PaidAt datetime2(7) NULL;
GO

/* ===== 015_ThanhToan_PaymentGateway.sql #2 ===== */
DECLARE @HadPaymentMethodCheck bit = CASE WHEN EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
      AND definition LIKE N'%PhuongThuc%'
) THEN 1 ELSE 0 END;

DECLARE @DropChecks nvarchar(max) = N'';
SELECT @DropChecks += N'ALTER TABLE dbo.ThanhToan DROP CONSTRAINT '
    + QUOTENAME(name) + N';'
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
  AND definition LIKE N'%PhuongThuc%';

IF @DropChecks <> N''
    EXEC sys.sp_executesql @DropChecks;

IF @HadPaymentMethodCheck = 1
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
         AND name = N'CK_ThanhToan_PhuongThuc'
   )
    ALTER TABLE dbo.ThanhToan ADD CONSTRAINT CK_ThanhToan_PhuongThuc
        CHECK (PhuongThuc IS NULL OR RTRIM(PhuongThuc) IN
            (N'TienMat', N'ChuyenKhoan', N'VNPay', N'MoMo'));
GO

/* ===== 015_ThanhToan_PaymentGateway.sql #3 ===== */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ThanhToan')
      AND name = N'UX_ThanhToan_Gateway_GatewayTxnId'
)
    CREATE UNIQUE INDEX UX_ThanhToan_Gateway_GatewayTxnId
        ON dbo.ThanhToan(Gateway, GatewayTxnId)
        WHERE Gateway IS NOT NULL AND GatewayTxnId IS NOT NULL;
GO

/* ===== 016_LichKhoiHanh_SoCho.sql #1 ===== */
-- SQL Server / Azure SQL. Run in the application's existing database.
-- NULL retains Tour.Slkhach as the default capacity.
IF COL_LENGTH(N'dbo.LichKhoiHanh', N'SoCho') IS NULL
BEGIN
    ALTER TABLE dbo.LichKhoiHanh ADD SoCho INT NULL;
END;
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #1 ===== */
/* 63 tỉnh thành (chia 3 miền). Mỗi tỉnh: 5 điểm tham quan, 5 khu vui chơi, 5 quán ăn,
   4-5 khách sạn × 3 loại phòng. Không sửa 001. Chạy sau 017. */
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.TinhThanh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinhThanh (
        MaTinh nchar(20) NOT NULL CONSTRAINT PK_TinhThanh PRIMARY KEY,
        TenTinh nvarchar(100) NOT NULL,
        MaKhuVuc nchar(20) NOT NULL,
        CONSTRAINT FK_TinhThanh_KhuVuc FOREIGN KEY (MaKhuVuc) REFERENCES dbo.KhuVuc(MaKhuVuc)
    );
END;
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #2 ===== */
IF COL_LENGTH(N'dbo.DiemThamQuan', N'MaTinh') IS NULL
    ALTER TABLE dbo.DiemThamQuan ADD MaTinh nchar(20) NULL;
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #3 ===== */
IF COL_LENGTH(N'dbo.DoiTac', N'MaTinh') IS NULL
    ALTER TABLE dbo.DoiTac ADD MaTinh nchar(20) NULL;
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #4 ===== */
IF COL_LENGTH(N'dbo.LichTrinhDeXuatChiTiet', N'GioBatDau') IS NULL
    ALTER TABLE dbo.LichTrinhDeXuatChiTiet ADD GioBatDau time(0) NULL;
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #5 ===== */
IF OBJECT_ID(N'dbo.FK_DiemThamQuan_TinhThanh', N'F') IS NULL
    ALTER TABLE dbo.DiemThamQuan ADD CONSTRAINT FK_DiemThamQuan_TinhThanh
        FOREIGN KEY (MaTinh) REFERENCES dbo.TinhThanh(MaTinh);
GO

/* ===== 018_TinhThanh_SampleCatalog.sql #6 ===== */
IF OBJECT_ID(N'dbo.FK_DoiTac_TinhThanh', N'F') IS NULL
    ALTER TABLE dbo.DoiTac ADD CONSTRAINT FK_DoiTac_TinhThanh
        FOREIGN KEY (MaTinh) REFERENCES dbo.TinhThanh(MaTinh);
GO

/* ===== 020_QuyenTaiKhoan.sql #1 ===== */
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

/* ===== 020_QuyenTaiKhoan.sql #2 ===== */
IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_QuyenNhanVien_MaUser'
          AND object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
BEGIN
    CREATE INDEX IX_QuyenNhanVien_MaUser ON dbo.QuyenNhanVien (MaUser);
END
GO

/* ===== 021_RefreshToken.sql #1 ===== */
/* Phiên đăng nhập có thể thu hồi. Chạy sau 020. Idempotent. */
IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        MaRefresh nchar(20) NOT NULL CONSTRAINT PK_RefreshToken PRIMARY KEY,
        MaUser nchar(20) NOT NULL,
        TokenHash char(64) NOT NULL,
        HetHan datetime2 NOT NULL,
        ThuHoiLuc datetime2 NULL,
        TaoLuc datetime2 NOT NULL CONSTRAINT DF_RefreshToken_TaoLuc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_RefreshToken_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshToken_NguoiSuDung FOREIGN KEY (MaUser)
            REFERENCES dbo.NguoiSuDung (MaUser)
    );
END
GO

/* ===== 021_RefreshToken.sql #2 ===== */
IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_RefreshToken_MaUser'
          AND object_id = OBJECT_ID(N'dbo.RefreshToken'))
BEGIN
    CREATE INDEX IX_RefreshToken_MaUser ON dbo.RefreshToken (MaUser);
END
GO

/* ===== 023_TuThietKeChatPlanner.sql #1 ===== */
-- 023: chat tự thiết kế, alias tỉnh, ma trận di chuyển, giờ lịch trình.
-- Không xóa user / booking / thanh toán. Chạy trên database đang dùng (Azure SQL).
-- Idempotent: có thể chạy lại.

-- ---------------------------------------------------------------------------
-- OPTIONAL RESET (bỏ comment nếu muốn xóa đề xuất/chat cũ, KHÔNG đụng vé)
-- ---------------------------------------------------------------------------
/*
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

/* ===== 023_TuThietKeChatPlanner.sql #2 ===== */
IF COL_LENGTH(N'dbo.LichTrinh', N'GioBatDau') IS NULL
    ALTER TABLE dbo.LichTrinh ADD GioBatDau time(0) NULL;
GO

/* ===== 023_TuThietKeChatPlanner.sql #3 ===== */
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

/* ===== 023_TuThietKeChatPlanner.sql #4 ===== */
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

/* ===== 023_TuThietKeChatPlanner.sql #5 ===== */
GO

/* ===== 023_TuThietKeChatPlanner.sql #6 ===== */
GO

/* ===== 023_TuThietKeChatPlanner.sql #7 ===== */
IF OBJECT_ID(N'dbo.FK_YeuCau_TinhXuatPhat', N'F') IS NULL
    ALTER TABLE dbo.YeuCauThietKe WITH NOCHECK
    ADD CONSTRAINT FK_YeuCau_TinhXuatPhat FOREIGN KEY (MaTinhXuatPhat) REFERENCES dbo.TinhThanh(MaTinh);
IF OBJECT_ID(N'dbo.FK_YeuCau_TinhDen', N'F') IS NULL
    ALTER TABLE dbo.YeuCauThietKe WITH NOCHECK
    ADD CONSTRAINT FK_YeuCau_TinhDen FOREIGN KEY (MaTinhDen) REFERENCES dbo.TinhThanh(MaTinh);
GO

/* ===== 024_MatchToanBoOffline.sql #1 ===== */
-- 024: khớp mọi tỉnh không cần Google Maps.
-- Alias đầy đủ + backfill MaTinh còn thiếu. Thời gian đi mọi cặp tỉnh do C# (Haversine).
-- Idempotent. Chạy sau 018 + 023. Không xóa user/booking.

IF OBJECT_ID(N'dbo.TinhThanhAlias', N'U') IS NULL
    THROW 50001, N'Chạy 023_TuThietKeChatPlanner.sql trước 024.', 1;
GO

/* ===== 024_MatchToanBoOffline.sql #2 ===== */
IF OBJECT_ID(N'dbo.MatranDiChuyen', N'U') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Matran_PhuongTien')
    ALTER TABLE dbo.MatranDiChuyen DROP CONSTRAINT CK_Matran_PhuongTien;
GO

/* ===== 024_MatchToanBoOffline.sql #3 ===== */
IF OBJECT_ID(N'dbo.MatranDiChuyen', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Matran_PhuongTien')
    ALTER TABLE dbo.MatranDiChuyen WITH NOCHECK
    ADD CONSTRAINT CK_Matran_PhuongTien CHECK (RTRIM(PhuongTien) IN (N'MayBay', N'Tau', N'XeKhach', N'XeMay'));
GO


/* ===== 026_SeedMauDayDu.sql #1 ===== */
﻿/* 026: Seed mẫu đầy đủ — không NULL, giá > 0 (nghìn đồng).
   Chạy SAU 025 trên cùng database Azure. SSMS, UTF-8.
   Mật khẩu mọi tài khoản: Test@123456
   VaiTro (3) và KhuVuc (3) giữ nguyên vì ràng buộc code.
   Nếu lần trước 026 lỗi: chạy lại 025 rồi mới chạy file này. */
SET NOCOUNT ON;
GO

/* ===== 026_SeedMauDayDu.sql #90 ===== */
/* Kiểm tra nhanh */
SELECT 'TinhThanh' AS Bang, COUNT(*) AS SoDong FROM dbo.TinhThanh
UNION ALL SELECT 'DiemThamQuan', COUNT(*) FROM dbo.DiemThamQuan
UNION ALL SELECT 'DoiTac', COUNT(*) FROM dbo.DoiTac
UNION ALL SELECT 'SanPhamDoiTac', COUNT(*) FROM dbo.SanPhamDoiTac
UNION ALL SELECT 'Tour', COUNT(*) FROM dbo.Tour
UNION ALL SELECT 'AnhTour', COUNT(*) FROM dbo.AnhTour
UNION ALL SELECT 'NguoiSuDung', COUNT(*) FROM dbo.NguoiSuDung
UNION ALL SELECT 'GiaMinSanPham', MIN(GiaNiemYet) FROM dbo.SanPhamDoiTac
UNION ALL SELECT 'GiaMaxSanPham', MAX(GiaNiemYet) FROM dbo.SanPhamDoiTac;
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #1 ===== */
/* 027: Đánh giá công khai vs nội bộ + KPI admin.
   Chạy SAU 026. SSMS UTF-8. Không xóa AnhTour / catalog.
   CongKhai=1: hiện website khách. =0: feedback tự thiết kế (nội bộ).
   Module QuyenNhanVien DanhGia: Them=1 chỉ để được XEM; admin không thêm/sửa/xóa bài. */
SET NOCOUNT ON;
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #2 ===== */
IF COL_LENGTH(N'dbo.DanhGiaTour', N'CongKhai') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGiaTour ADD CongKhai bit NOT NULL
        CONSTRAINT DF_DanhGiaTour_CongKhai DEFAULT (1);
END
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #3 ===== */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DanhGiaTour_CongKhai_ThoiGian'
      AND object_id = OBJECT_ID(N'dbo.DanhGiaTour'))
    CREATE INDEX IX_DanhGiaTour_CongKhai_ThoiGian
        ON dbo.DanhGiaTour (CongKhai, ThoiGian DESC);
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #4 ===== */
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #5 ===== */
IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
    CHECK (RTRIM(ChucNang) IN (
        N'TongQuan', N'Tour', N'Booking', N'UuDai',
        N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia', N'TaiKhoan'));
GO

/* ===== 027_DanhGiaCongKhaiVaNoiBo.sql #9 ===== */
SELECT N'DanhGiaTour' AS Bang, COUNT(*) AS SoDong,
       SUM(CASE WHEN CongKhai = 1 THEN 1 ELSE 0 END) AS CongKhai,
       SUM(CASE WHEN CongKhai = 0 THEN 1 ELSE 0 END) AS NoiBo,
       CAST(AVG(CAST(SaoDanhGia AS float)) AS decimal(4,2)) AS DiemTb
FROM dbo.DanhGiaTour;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #1 ===== */
/* 028: hạn sửa đánh giá 5 ngày, media Cloudinary, ảnh giấy tờ, quyền quản lý khách.
   Idempotent. Chạy trên Azure sau 027. Không USE. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.DanhGiaTour', N'ThoiGianSua') IS NULL
    ALTER TABLE dbo.DanhGiaTour ADD ThoiGianSua datetime NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #2 ===== */
IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'CloudPublicId') IS NULL
    ALTER TABLE dbo.MediaDanhGiaTour ADD CloudPublicId nvarchar(200) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #3 ===== */
IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'CloudResourceType') IS NULL
    ALTER TABLE dbo.MediaDanhGiaTour ADD CloudResourceType nvarchar(20) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #4 ===== */
IF COL_LENGTH(N'dbo.MediaDanhGiaTour', N'Url') IS NOT NULL
    ALTER TABLE dbo.MediaDanhGiaTour ALTER COLUMN Url nvarchar(500) NOT NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #5 ===== */
IF COL_LENGTH(N'dbo.GiayTo', N'AnhMatTruoc') IS NULL
    ALTER TABLE dbo.GiayTo ADD AnhMatTruoc nvarchar(500) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #6 ===== */
IF COL_LENGTH(N'dbo.GiayTo', N'AnhMatSau') IS NULL
    ALTER TABLE dbo.GiayTo ADD AnhMatSau nvarchar(500) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #7 ===== */
IF COL_LENGTH(N'dbo.GiayTo', N'CloudPublicIdTruoc') IS NULL
    ALTER TABLE dbo.GiayTo ADD CloudPublicIdTruoc nvarchar(200) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #8 ===== */
IF COL_LENGTH(N'dbo.GiayTo', N'CloudPublicIdSau') IS NULL
    ALTER TABLE dbo.GiayTo ADD CloudPublicIdSau nvarchar(200) NULL;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #9 ===== */
IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang')
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #10 ===== */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang')
    ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
    CHECK (ChucNang IN (N'TongQuan', N'Tour', N'Booking', N'UuDai',
        N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia', N'KhachHang', N'TaiKhoan'));
GO

/* ===== 028_ReviewEditMediaVaKhachHang.sql #12 ===== */
SELECT N'DanhGiaTour.ThoiGianSua' AS Cot, CASE WHEN COL_LENGTH(N'dbo.DanhGiaTour', N'ThoiGianSua') IS NULL THEN 0 ELSE 1 END AS CoCot
UNION ALL
SELECT N'GiayTo.AnhMatTruoc', CASE WHEN COL_LENGTH(N'dbo.GiayTo', N'AnhMatTruoc') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'Quyen KhachHang', COUNT(*) FROM dbo.QuyenNhanVien WHERE ChucNang = N'KhachHang';
GO

/* ===== 028b_FixMaQuyen.sql #1 ===== */
/* Chạy nếu 028 cũ đã thêm cột nhưng INSERT quyền bị lỗi MaQuyen NULL. Idempotent. */
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO

/* ===== 028b_FixMaQuyen.sql #2 ===== */
ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
CHECK (RTRIM(ChucNang) IN (N'TongQuan', N'Tour', N'Booking', N'UuDai',
    N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia', N'KhachHang', N'TaiKhoan'));
GO

/* ===== 028b_FixMaQuyen.sql #4 ===== */
SELECT COUNT(*) AS SoQuyenKhachHang FROM dbo.QuyenNhanVien WHERE ChucNang = N'KhachHang';
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #1 ===== */
/* 029: hồ sơ nhân viên, xóa tài khoản (VoHieu), chat khách–admin, module HoTro.
   Idempotent. Chạy sau 028. Không USE. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.NguoiSuDung', N'TrangThai') IS NULL
    ALTER TABLE dbo.NguoiSuDung ADD TrangThai nchar(20) NOT NULL
        CONSTRAINT DF_NguoiSuDung_TrangThai DEFAULT (N'HoatDong');
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #2 ===== */
IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhanVien
    (
        MaNhanVien nchar(20) NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,
        MaUser nchar(20) NOT NULL CONSTRAINT UQ_NhanVien_MaUser UNIQUE,
        Ho nvarchar(50) NOT NULL,
        Ten nvarchar(50) NOT NULL,
        SoCccd nvarchar(20) NOT NULL,
        ChucVu nvarchar(40) NOT NULL,
        CONSTRAINT FK_NhanVien_NguoiSuDung FOREIGN KEY (MaUser)
            REFERENCES dbo.NguoiSuDung (MaUser)
    );
    CREATE INDEX IX_NhanVien_SoCccd ON dbo.NhanVien (SoCccd);
END
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #3 ===== */
IF OBJECT_ID(N'dbo.CuocTroChuyen', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CuocTroChuyen
    (
        MaCuoc nchar(20) NOT NULL CONSTRAINT PK_CuocTroChuyen PRIMARY KEY,
        MaUserKhach nchar(20) NOT NULL,
        MaUserNhanVien nchar(20) NULL,
        TieuDe nvarchar(200) NOT NULL,
        TrangThai nvarchar(20) NOT NULL CONSTRAINT DF_CuocTroChuyen_TrangThai DEFAULT (N'Mo'),
        ThoiGianTao datetime NOT NULL CONSTRAINT DF_CuocTroChuyen_Tao DEFAULT (SYSUTCDATETIME()),
        ThoiGianCapNhat datetime NOT NULL CONSTRAINT DF_CuocTroChuyen_CapNhat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_CuocTroChuyen_Khach FOREIGN KEY (MaUserKhach)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT FK_CuocTroChuyen_NhanVien FOREIGN KEY (MaUserNhanVien)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT CK_CuocTroChuyen_TrangThai CHECK (RTRIM(TrangThai) IN (N'Mo', N'Dong'))
    );
    CREATE INDEX IX_CuocTroChuyen_Khach ON dbo.CuocTroChuyen (MaUserKhach, ThoiGianCapNhat DESC);
    CREATE INDEX IX_CuocTroChuyen_NV ON dbo.CuocTroChuyen (MaUserNhanVien, ThoiGianCapNhat DESC);
END
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #4 ===== */
IF OBJECT_ID(N'dbo.TinNhanHoTro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinNhanHoTro
    (
        MaTinNhan nchar(20) NOT NULL CONSTRAINT PK_TinNhanHoTro PRIMARY KEY,
        MaCuoc nchar(20) NOT NULL,
        MaUserGui nchar(20) NOT NULL,
        VaiTroGui nvarchar(20) NOT NULL,
        NoiDung nvarchar(2000) NOT NULL,
        ThoiGian datetime NOT NULL CONSTRAINT DF_TinNhanHoTro_ThoiGian DEFAULT (SYSUTCDATETIME()),
        DaDoc bit NOT NULL CONSTRAINT DF_TinNhanHoTro_DaDoc DEFAULT (0),
        CONSTRAINT FK_TinNhanHoTro_Cuoc FOREIGN KEY (MaCuoc)
            REFERENCES dbo.CuocTroChuyen (MaCuoc) ON DELETE CASCADE,
        CONSTRAINT FK_TinNhanHoTro_User FOREIGN KEY (MaUserGui)
            REFERENCES dbo.NguoiSuDung (MaUser),
        CONSTRAINT CK_TinNhanHoTro_VaiTro CHECK (RTRIM(VaiTroGui) IN (N'KhachHang', N'NhanVien'))
    );
    CREATE INDEX IX_TinNhanHoTro_Cuoc ON dbo.TinNhanHoTro (MaCuoc, ThoiGian);
END
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #5 ===== */
IF EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #6 ===== */
IF OBJECT_ID(N'dbo.QuyenNhanVien', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien WITH NOCHECK ADD CONSTRAINT CK_QuyenNhanVien_ChucNang
    CHECK (RTRIM(ChucNang) IN (
        N'TongQuan', N'Tour', N'Booking', N'UuDai',
        N'DiemThamQuan', N'DoiTac', N'ThietKe', N'DanhGia',
        N'KhachHang', N'TaiKhoan', N'HoTro'));
GO

/* ===== 029_TaiKhoanNhanVienVaHoTro.sql #8 ===== */
SELECT N'NhanVien' AS Muc, CASE WHEN OBJECT_ID(N'dbo.NhanVien') IS NULL THEN 0 ELSE 1 END AS GiaTri
UNION ALL
SELECT N'CuocTroChuyen', CASE WHEN OBJECT_ID(N'dbo.CuocTroChuyen') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'TinNhanHoTro', CASE WHEN OBJECT_ID(N'dbo.TinNhanHoTro') IS NULL THEN 0 ELSE 1 END
UNION ALL
SELECT N'Quyen HoTro', COUNT(*) FROM dbo.QuyenNhanVien WHERE ChucNang = N'HoTro';
GO

/* ===== 030_DiaChiLoaiDongAnUong.sql #1 ===== */
-- 030: địa chỉ đối tác, loại dòng lịch trình, nhà hàng mỗi tỉnh, ma trận bay đảo.
-- Idempotent. Chạy sau 029. KHÔNG chạy 025.

IF COL_LENGTH('dbo.DoiTac', 'DiaChi') IS NULL
    ALTER TABLE dbo.DoiTac ADD DiaChi nvarchar(200) NULL;
IF COL_LENGTH('dbo.LichTrinh', 'LoaiDong') IS NULL
    ALTER TABLE dbo.LichTrinh ADD LoaiDong nchar(20) NULL;
IF COL_LENGTH('dbo.LichTrinhDeXuatChiTiet', 'LoaiDong') IS NULL
    ALTER TABLE dbo.LichTrinhDeXuatChiTiet ADD LoaiDong nchar(20) NULL;
GO


