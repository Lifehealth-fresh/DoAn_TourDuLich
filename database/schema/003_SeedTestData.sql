/* Dữ liệu test tối thiểu. Mật khẩu tất cả tài khoản: Test@123456 */
DECLARE @Hash nvarchar(200) = N'$2a$11$DOcyrWZ/bvqL3mWaqhbnKujUSEGbAuRkSR7pxYf4/LasW9DseeQFe';

INSERT INTO dbo.KhuVuc (MaKhuVuc, TenKhuVuc, QuocGia, ViDo, KinhDo, MuiGio, TrangThai)
SELECT v.MaKhuVuc, v.TenKhuVuc, v.QuocGia, v.ViDo, v.KinhDo, v.MuiGio, v.TrangThai
FROM (VALUES
    (N'KV001', N'Miền Bắc', N'Việt Nam', CAST(21.028511 AS decimal(9,6)), CAST(105.804817 AS decimal(9,6)), N'UTC+07:00', N'HoatDong'),
    (N'KV002', N'Miền Trung', N'Việt Nam', CAST(16.054407 AS decimal(9,6)), CAST(108.202164 AS decimal(9,6)), N'UTC+07:00', N'HoatDong'),
    (N'KV003', N'Miền Nam', N'Việt Nam', CAST(10.823099 AS decimal(9,6)), CAST(106.629662 AS decimal(9,6)), N'UTC+07:00', N'HoatDong')
) v(MaKhuVuc, TenKhuVuc, QuocGia, ViDo, KinhDo, MuiGio, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KhuVuc x WHERE x.MaKhuVuc = v.MaKhuVuc);

INSERT INTO dbo.DiemThamQuan (MaDThamQuan, TenDiaDanh, DiaChi, MaKhuVuc, KinhDo, ViDo, Mota)
SELECT v.MaDThamQuan, v.TenDiaDanh, v.DiaChi, v.MaKhuVuc, v.KinhDo, v.ViDo, v.Mota
FROM (VALUES
    (N'DT001', N'Hồ Hoàn Kiếm', N'Đinh Tiên Hoàng, Hà Nội', N'KV001', CAST(105.852000 AS decimal(9,6)), CAST(21.028700 AS decimal(9,6)), N'Khu vực trung tâm Hà Nội'),
    (N'DT002', N'Vịnh Hạ Long', N'Thành phố Hạ Long, Quảng Ninh', N'KV001', CAST(107.084300 AS decimal(9,6)), CAST(20.910100 AS decimal(9,6)), N'Di sản thiên nhiên thế giới'),
    (N'DT003', N'Phố cổ Hội An', N'Thành phố Hội An, Quảng Nam', N'KV002', CAST(108.338000 AS decimal(9,6)), CAST(15.880100 AS decimal(9,6)), N'Khu phố cổ lịch sử'),
    (N'DT004', N'Bảo tàng Chăm', N'Đà Nẵng', N'KV002', CAST(108.218800 AS decimal(9,6)), CAST(16.060600 AS decimal(9,6)), N'Bảo tàng điêu khắc Chăm')
) v(MaDThamQuan, TenDiaDanh, DiaChi, MaKhuVuc, KinhDo, ViDo, Mota)
WHERE NOT EXISTS (SELECT 1 FROM dbo.DiemThamQuan x WHERE x.MaDThamQuan = v.MaDThamQuan);

INSERT INTO dbo.DoiTac (MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, MaKhuVuc, PhanTramHoaHong, TrangThai)
SELECT v.MaDoiTac, v.TenDoiTac, v.LoaiDoiTac, v.NguoiLienHe, v.SoDienThoai, v.Email, v.MaKhuVuc, v.PhanTramHoaHong, v.TrangThai
FROM (VALUES
    (N'DTAC001', N'Khách sạn Hoàn Kiếm', N'LuuTru', N'Nguyễn An', N'0901000001', N'hk@example.com', N'KV001', CAST(10.00 AS decimal(5,2)), N'HoatDong'),
    (N'DTAC002', N'Vận tải Miền Trung', N'VanChuyen', N'Trần Bình', N'0901000002', N'vt@example.com', N'KV002', CAST(8.50 AS decimal(5,2)), N'HoatDong'),
    (N'DTAC003', N'Ẩm thực Phố Hội', N'AnUong', N'Lê Chi', N'0901000003', N'an@example.com', N'KV002', CAST(12.00 AS decimal(5,2)), N'HoatDong'),
    (N'DTAC004', N'Trải nghiệm Việt', N'HoatDong', N'Phạm Dũng', N'0901000004', N'hd@example.com', N'KV001', CAST(15.00 AS decimal(5,2)), N'HoatDong')
) v(MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, MaKhuVuc, PhanTramHoaHong, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.DoiTac x WHERE x.MaDoiTac = v.MaDoiTac);

INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
SELECT v.MaSanPham, v.MaDoiTac, v.TenSanPham, v.DonViTinh, v.GiaNiemYet, v.MaDThamQuan, v.Mota, v.TrangThai
FROM (VALUES
    (N'SP001', N'DTAC001', N'Phòng tiêu chuẩn', N'phòng', 800000, N'DT001', N'Phòng nghỉ trung tâm', N'HoatDong'),
    (N'SP002', N'DTAC001', N'Phòng hướng hồ', N'phòng', 1200000, N'DT001', N'Phòng nghỉ cao cấp', N'HoatDong'),
    (N'SP003', N'DTAC002', N'Xe đưa đón sân bay', N'chuyến', 450000, N'DT003', N'Dịch vụ xe riêng', N'HoatDong'),
    (N'SP004', N'DTAC002', N'Xe tham quan 16 chỗ', N'ngày', 1500000, N'DT004', N'Xe du lịch', N'HoatDong'),
    (N'SP005', N'DTAC003', N'Bữa tối đặc sản Hội An', N'suất', 350000, N'DT003', N'Thực đơn địa phương', N'HoatDong'),
    (N'SP006', N'DTAC003', N'Lớp học nấu ăn', N'người', 600000, N'DT003', N'Trải nghiệm ẩm thực', N'HoatDong'),
    (N'SP007', N'DTAC004', N'Tour thuyền Vịnh Hạ Long', N'người', 900000, N'DT002', N'Tham quan bằng thuyền', N'HoatDong'),
    (N'SP008', N'DTAC004', N'Vé tham quan bảo tàng', N'vé', 120000, N'DT004', N'Vé vào cửa', N'HoatDong')
) v(MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SanPhamDoiTac x WHERE x.MaSanPham = v.MaSanPham);

INSERT INTO dbo.Tour (MaTour, TenTour, Mota, ThoiGian, DieuKhoan, GiaTour, SLKhach, SLHuongDanVien, LoaiTour, TrangThai)
SELECT v.MaTour, v.TenTour, v.Mota, v.ThoiGian, v.DieuKhoan, v.GiaTour, v.SLKhach, v.SLHuongDanVien, v.LoaiTour, v.TrangThai
FROM (VALUES
    (N'TOUR001', N'Hà Nội - Hạ Long', N'Hành trình miền Bắc tiêu biểu', 3, N'Tuân thủ lịch trình của đoàn.', 3500000, 30, 1, N'Chuan', N'HoatDong'),
    (N'TOUR002', N'Đà Nẵng - Hội An', N'Hành trình di sản miền Trung', 3, N'Tuân thủ lịch trình của đoàn.', 4200000, 25, 1, N'Chuan', N'HoatDong')
) v(MaTour, TenTour, Mota, ThoiGian, DieuKhoan, GiaTour, SLKhach, SLHuongDanVien, LoaiTour, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Tour x WHERE x.MaTour = v.MaTour);

INSERT INTO dbo.LichTrinh (MaLichTrinh, MaTour, NgayThu, ThuTuTrongNgay, MaDThamQuan, MaSanPham, SoLuong, DonGia, ThoiGianDuKien, Mota)
SELECT v.MaLichTrinh, v.MaTour, v.NgayThu, v.ThuTuTrongNgay, v.MaDThamQuan, v.MaSanPham, v.SoLuong, v.DonGia, v.ThoiGianDuKien, v.Mota
FROM (VALUES
    (N'LT001', N'TOUR001', 1, 1, N'DT001', NULL, 1, 0, DATEADD(HOUR, 9, CAST(GETUTCDATE() AS datetime)), N'Tham quan Hồ Hoàn Kiếm'),
    (N'LT002', N'TOUR001', 2, 1, N'DT002', N'SP007', 1, 900000, DATEADD(DAY, 1, CAST(GETUTCDATE() AS datetime)), N'Tham quan Vịnh Hạ Long'),
    (N'LT003', N'TOUR002', 1, 1, N'DT003', N'SP005', 1, 350000, DATEADD(HOUR, 9, CAST(GETUTCDATE() AS datetime)), N'Tham quan Phố cổ Hội An'),
    (N'LT004', N'TOUR002', 2, 1, N'DT004', N'SP008', 1, 120000, DATEADD(DAY, 1, CAST(GETUTCDATE() AS datetime)), N'Tham quan Bảo tàng Chăm')
) v(MaLichTrinh, MaTour, NgayThu, ThuTuTrongNgay, MaDThamQuan, MaSanPham, SoLuong, DonGia, ThoiGianDuKien, Mota)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LichTrinh x WHERE x.MaLichTrinh = v.MaLichTrinh);

INSERT INTO dbo.LichKhoiHanh (MaKhoiHanh, MaTour, NgayKhoiHanh, NgayKetThuc, DiaDiem)
SELECT v.MaKhoiHanh, v.MaTour, v.NgayKhoiHanh, v.NgayKetThuc, v.DiaDiem
FROM (VALUES
    (N'KH001', N'TOUR001', DATEADD(DAY, 30, GETUTCDATE()), DATEADD(DAY, 32, GETUTCDATE()), N'Hà Nội'),
    (N'KH002', N'TOUR002', DATEADD(DAY, 45, GETUTCDATE()), DATEADD(DAY, 47, GETUTCDATE()), N'Đà Nẵng')
) v(MaKhoiHanh, MaTour, NgayKhoiHanh, NgayKetThuc, DiaDiem)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LichKhoiHanh x WHERE x.MaKhoiHanh = v.MaKhoiHanh);

INSERT INTO dbo.HuongDanVien (MaHuongDanVien, HoTen, NgaySinh, QueQuan, Email, CCCD, SoDienThoai)
SELECT v.MaHuongDanVien, v.HoTen, v.NgaySinh, v.QueQuan, v.Email, v.CCCD, v.SoDienThoai
FROM (VALUES
    (N'HDV001', N'Nguyễn Hướng Dẫn', CAST('1990-05-10' AS date), N'Hà Nội', N'hdv1@example.com', N'001090000001', N'0912000001'),
    (N'HDV002', N'Trần Du Lịch', CAST('1992-08-20' AS date), N'Đà Nẵng', N'hdv2@example.com', N'001092000002', N'0912000002')
) v(MaHuongDanVien, HoTen, NgaySinh, QueQuan, Email, CCCD, SoDienThoai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.HuongDanVien x WHERE x.MaHuongDanVien = v.MaHuongDanVien);

INSERT INTO dbo.LichDanTour (MaLichDanTour, MaHDV, MaTour, MaKhoiHanh)
SELECT v.MaLichDanTour, v.MaHDV, v.MaTour, v.MaKhoiHanh
FROM (VALUES
    (N'LDT001', N'HDV001', N'TOUR001', N'KH001'),
    (N'LDT002', N'HDV002', N'TOUR002', N'KH002')
) v(MaLichDanTour, MaHDV, MaTour, MaKhoiHanh)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LichDanTour x WHERE x.MaLichDanTour = v.MaLichDanTour);

IF NOT EXISTS (SELECT 1 FROM dbo.NhomKhuyenMai WHERE MaNhomKM = N'NKM001')
    INSERT INTO dbo.NhomKhuyenMai (MaNhomKM, TenNhomKM) VALUES (N'NKM001', N'Khuyến mãi khách mới');

IF NOT EXISTS (SELECT 1 FROM dbo.KhuyenMai WHERE MaKM = N'KM001')
    INSERT INTO dbo.KhuyenMai (MaKM, MaNhomKM, TenKM, MaCode, NgayBD, NgayKT, DonVi, GiamGia, CoCongDon, TrangThai)
    VALUES (N'KM001', N'NKM001', N'Giảm giá mùa hè', N'SUMMER001', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, 90, GETUTCDATE()), N'%', 10, 0, N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.DieuKienKM WHERE MaDK = N'DK001')
    INSERT INTO dbo.DieuKienKM (MaDK, MaKhuyenMai, DonToiThieu, LanDatDau, SoLuong)
    VALUES (N'DK001', N'KM001', 1000000, 1, 100);

IF NOT EXISTS (SELECT 1 FROM dbo.KM_Tour WHERE STT = 1)
    INSERT INTO dbo.KM_Tour (STT, MaKhuyenMai, MaTour) VALUES (1, N'KM001', N'TOUR001');

IF NOT EXISTS (SELECT 1 FROM dbo.NguoiSuDung WHERE MaUser = N'USRADMIN001')
    INSERT INTO dbo.NguoiSuDung (MaUser, SoDienThoai, MatKhau, MaVaiTro)
    SELECT N'USRADMIN001', N'0900000001', @Hash, MaVaiTro FROM dbo.VaiTro WHERE TenVaiTro = N'Admin';

IF NOT EXISTS (SELECT 1 FROM dbo.NguoiSuDung WHERE MaUser = N'USRSALE0001')
    INSERT INTO dbo.NguoiSuDung (MaUser, SoDienThoai, MatKhau, MaVaiTro)
    SELECT N'USRSALE0001', N'0900000002', @Hash, MaVaiTro FROM dbo.VaiTro WHERE TenVaiTro = N'Sale';

IF NOT EXISTS (SELECT 1 FROM dbo.NguoiSuDung WHERE MaUser = N'USRKH000001')
    INSERT INTO dbo.NguoiSuDung (MaUser, SoDienThoai, MatKhau, MaVaiTro)
    SELECT N'USRKH000001', N'0900000003', @Hash, MaVaiTro FROM dbo.VaiTro WHERE TenVaiTro = N'KhachHang';

IF NOT EXISTS (SELECT 1 FROM dbo.NguoiSuDung WHERE MaUser = N'USRKH000002')
    INSERT INTO dbo.NguoiSuDung (MaUser, SoDienThoai, MatKhau, MaVaiTro)
    SELECT N'USRKH000002', N'0900000004', @Hash, MaVaiTro FROM dbo.VaiTro WHERE TenVaiTro = N'KhachHang';

IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WHERE MaKhachHang = N'KHACH001')
    INSERT INTO dbo.KhachHang (MaKhachHang, Ho, Ten, HoGiayTo, TenGiayTo, QuocTich, DanhXung, GioiTinh, NgaySinh, Email, SoDienThoai, MaUser)
    VALUES (N'KHACH001', N'Nguyễn', N'An', N'Nguyễn', N'An', N'Việt Nam', N'Anh', N'Nam', '1998-01-15', N'an.test@example.com', N'0900000003', N'USRKH000001');

IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WHERE MaKhachHang = N'KHACH002')
    INSERT INTO dbo.KhachHang (MaKhachHang, Ho, Ten, HoGiayTo, TenGiayTo, QuocTich, DanhXung, GioiTinh, NgaySinh, Email, SoDienThoai, MaUser)
    VALUES (N'KHACH002', N'Trần', N'Bình', N'Trần', N'Bình', N'Việt Nam', N'Chị', N'Nữ', '1999-06-20', N'binh.test@example.com', N'0900000004', N'USRKH000002');

IF NOT EXISTS (SELECT 1 FROM dbo.GiayTo WHERE MaGiayTo = N'GT001')
    INSERT INTO dbo.GiayTo (MaGiayTo, LoaiGiayTo, SoTrenGiayTo, NgayCap, NgayHetHan, NoiCap, MaKhachHang)
    VALUES (N'GT001', N'CCCD', N'079098000001', '2020-01-01', '2035-01-01', N'Cục CSQLHC', N'KHACH001');

IF NOT EXISTS (SELECT 1 FROM dbo.GiayTo WHERE MaGiayTo = N'GT002')
    INSERT INTO dbo.GiayTo (MaGiayTo, LoaiGiayTo, SoTrenGiayTo, NgayCap, NgayHetHan, NoiCap, MaKhachHang)
    VALUES (N'GT002', N'CCCD', N'079099000002', '2021-02-01', '2036-02-01', N'Cục CSQLHC', N'KHACH002');

IF NOT EXISTS (SELECT 1 FROM dbo.AIGoiY WHERE MaRecommodation = N'GOIY000001')
    INSERT INTO dbo.AIGoiY (MaRecommodation, MaUser, MaTour, DiemPhuHop, LyDo, NgayGoiY)
    VALUES (N'GOIY000001', N'USRKH000001', N'TOUR001', 0.92, N'Phù hợp sở thích tham quan miền Bắc', GETUTCDATE());
GO
