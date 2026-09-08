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

CREATE TABLE dbo.HuongDanVien
(
    MaHuongDanVien nchar(20) NOT NULL CONSTRAINT PK_HuongDanVien PRIMARY KEY,
    HoTen nvarchar(50) NULL,
    NgaySinh date NULL,
    QueQuan nvarchar(100) NULL,
    Email nvarchar(50) NULL,
    CCCD nvarchar(30) NULL,
    SoDienThoai nchar(20) NULL
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

CREATE TABLE dbo.LichDanTour
(
    MaLichDanTour nchar(20) NOT NULL CONSTRAINT PK_LichDanTour PRIMARY KEY,
    MaHDV nchar(20) NOT NULL,
    MaTour nchar(20) NOT NULL,
    MaKhoiHanh nchar(20) NOT NULL,
    CONSTRAINT FK_LichDanTour_HDV FOREIGN KEY (MaHDV) REFERENCES dbo.HuongDanVien(MaHuongDanVien),
    CONSTRAINT FK_LichDanTour_KhoiHanh FOREIGN KEY (MaKhoiHanh) REFERENCES dbo.LichKhoiHanh(MaKhoiHanh),
    CONSTRAINT FK_LichDanTour_Tour FOREIGN KEY (MaTour) REFERENCES dbo.Tour(MaTour)
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

CREATE TABLE dbo.DanhGiaHDV
(
    MaDanhGiaHDV nchar(20) NOT NULL CONSTRAINT PK_DanhGiaHDV PRIMARY KEY,
    MaUser nchar(20) NOT NULL,
    MaHDV nchar(20) NOT NULL,
    ThoiGian datetime NOT NULL,
    SaoDanhGia int NOT NULL,
    NhanXet nvarchar(max) NULL,
    CONSTRAINT FK_DanhGiaHDV_HDV FOREIGN KEY (MaHDV) REFERENCES dbo.HuongDanVien(MaHuongDanVien),
    CONSTRAINT FK_DanhGiaHDV_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.DanhGiaSanPhamDoiTac
(
    MaDanhGia nchar(20) NOT NULL CONSTRAINT PK_DanhGiaSanPhamDoiTac PRIMARY KEY,
    MaSanPham nchar(20) NOT NULL,
    MaUser nchar(20) NOT NULL,
    ThoiGian datetime NOT NULL,
    SaoDanhGia int NOT NULL,
    NhanXet nvarchar(max) NULL,
    CONSTRAINT FK_DanhGiaSPDT_SanPham FOREIGN KEY (MaSanPham) REFERENCES dbo.SanPhamDoiTac(MaSanPham),
    CONSTRAINT FK_DanhGiaSPDT_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
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

CREATE TABLE dbo.ThongBao
(
    MaThongBao nchar(20) NOT NULL CONSTRAINT PK_ThongBao PRIMARY KEY,
    MaUser nchar(20) NULL,
    TieuDe nvarchar(50) NULL,
    NoiDung nvarchar(max) NULL,
    DaDoc bit NULL,
    NgayGui date NULL,
    CONSTRAINT FK_ThongBao_NguoiSuDung FOREIGN KEY (MaUser) REFERENCES dbo.NguoiSuDung(MaUser)
);

CREATE TABLE dbo.Quyen
(
    MaQuyen int NOT NULL CONSTRAINT PK_Quyen PRIMARY KEY,
    TenQuyen nvarchar(50) NOT NULL,
    Mota nvarchar(100) NULL,
    MaVaiTro int NOT NULL,
    CONSTRAINT FK_Quyen_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES dbo.VaiTro(MaVaiTro)
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
