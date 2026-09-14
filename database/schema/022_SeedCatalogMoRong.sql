/* Bổ sung tour đang bán, lịch khởi hành (có sức chứa), ưu đãi.
   Không đụng mật khẩu seed. Ảnh tour để trống — upload sau trên trang admin.
   Chạy sau 021. Idempotent: mỗi dòng tự bỏ qua nếu đã có.
   Tương thích KM_Tour.STT identity (Azure) và không identity (script 001). */

SET NOCOUNT ON;

/* Sức chứa các lịch đã có (016 thêm cột SoCho). */
UPDATE dbo.LichKhoiHanh SET SoCho = 30 WHERE MaKhoiHanh = N'KH001' AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 25 WHERE MaKhoiHanh = N'KH002' AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 28 WHERE MaKhoiHanh IN (N'KH003', N'KH004') AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 22 WHERE MaKhoiHanh IN (N'KH005', N'KH006') AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 26 WHERE MaKhoiHanh IN (N'KH007', N'KH008') AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 24 WHERE MaKhoiHanh = N'KH009' AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 30 WHERE MaKhoiHanh = N'KH010' AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 20 WHERE MaKhoiHanh IN (N'KH011', N'KH012') AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 26 WHERE MaKhoiHanh = N'KH013' AND SoCho IS NULL;
UPDATE dbo.LichKhoiHanh SET SoCho = 28 WHERE MaKhoiHanh IN (N'KH014', N'KH015') AND SoCho IS NULL;

INSERT INTO dbo.Tour (MaTour, TenTour, Mota, ThoiGian, DieuKhoan, GiaTour, SLKhach, SLHuongDanVien, LoaiTour, TrangThai)
SELECT v.MaTour, v.TenTour, v.Mota, v.ThoiGian, v.DieuKhoan, v.GiaTour, v.SLKhach, v.SLHuongDanVien, v.LoaiTour, v.TrangThai
FROM (VALUES
    (N'TOUR010', N'Hà Giang mùa vàng', N'Bốn ngày cao nguyên đá: đèo Mã Pí Lèng, sông Nho Quế, nhà trình tường. Phù hợp nhóm bạn thích phượt nhẹ.', 4, N'Gồm khách sạn/homestay, xe, bữa chính, vé thắng cảnh. Không gồm vé máy bay và thuê xe máy tự túc.', 5800000, 18, 1, N'Chuan', N'HoatDong'),
    (N'TOUR011', N'Mai Châu - Pù Luông', N'Ba ngày thung lũng Hòa Bình: nhà sàn, ruộng bậc thang, suối. Nhịp chậm, nhiều khoảng xanh.', 3, N'Gồm homestay, xe, bữa chính, trải nghiệm bản. Không gồm vé máy bay.', 3200000, 22, 1, N'Chuan', N'HoatDong'),
    (N'TOUR012', N'Đà Nẵng - Bà Nà - Sơn Trà', N'Ba ngày Đà Nẵng: Bà Nà Hills, bán đảo Sơn Trà, biển Mỹ Khê. Gia đình và cặp đôi.', 3, N'Gồm khách sạn, xe, vé Bà Nà, bữa chính. Không gồm vé máy bay.', 4900000, 28, 1, N'Chuan', N'HoatDong'),
    (N'TOUR013', N'Quy Nhơn - Kỳ Co', N'Ba ngày biển Bình Định: Kỳ Co, Eo Gió, tháp Chăm. Nước trong, ít đông hơn Nha Trang.', 3, N'Gồm khách sạn, xe, cano Kỳ Co, bữa chính. Không gồm vé máy bay.', 3600000, 24, 1, N'Chuan', N'HoatDong'),
    (N'TOUR014', N'Phan Thiết - Mũi Né', N'Ba ngày đồi cát và biển: Mũi Né, bàu Sen, hải sản đêm. Nắng, gió, phù hợp gia đình.', 3, N'Gồm khách sạn, xe, điểm check-in, bữa chính. Không gồm vé máy bay.', 3400000, 26, 1, N'Chuan', N'HoatDong'),
    (N'TOUR015', N'Vũng Tàu cuối tuần', N'Hai ngày biển gần Sài Gòn: Bãi Sau, tượng Chúa, hải sản. Đi nhanh, về kịp thứ Hai.', 2, N'Gồm khách sạn, xe khứ hồi, bữa chính. Không gồm vé cáp treo.', 2200000, 32, 1, N'Chuan', N'HoatDong'),
    (N'TOUR016', N'Tây Ninh - Núi Bà Đen', N'Hai ngày hành hương: cáp treo Núi Bà, Tòa Thánh Tây Ninh, bánh tráng phơi sương.', 2, N'Gồm khách sạn, xe, vé cáp treo, bữa chính.', 1900000, 30, 1, N'Chuan', N'HoatDong'),
    (N'TOUR017', N'Sài Gòn - Củ Chi', N'Hai ngày đô thị: Dinh Độc Lập, chợ Bến Thành, địa đạo Củ Chi. Phù hợp khách lần đầu.', 2, N'Gồm khách sạn trung tâm, xe, vé điểm đến, bữa chính.', 2500000, 28, 1, N'Chuan', N'HoatDong'),
    (N'TOUR018', N'Mộc Châu cao nguyên sữa', N'Ba ngày Sơn La: đồi chè, hoa mận, thác Dải Yếm. Se lạnh, nhiều ảnh.', 3, N'Gồm khách sạn, xe giường nằm, bữa chính, điểm check-in. Nên mang áo ấm.', 3900000, 22, 1, N'Chuan', N'HoatDong')
) v(MaTour, TenTour, Mota, ThoiGian, DieuKhoan, GiaTour, SLKhach, SLHuongDanVien, LoaiTour, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour);

INSERT INTO dbo.LichTrinh (MaLichTrinh, MaTour, NgayThu, ThuTuTrongNgay, MaDThamQuan, MaSanPham, SoLuong, DonGia, ThoiGianDuKien, Mota)
SELECT v.MaLichTrinh, v.MaTour, v.NgayThu, v.ThuTuTrongNgay, v.MaDThamQuan, v.MaSanPham, v.SoLuong, v.DonGia, v.ThoiGianDuKien, v.Mota
FROM (VALUES
    (N'LT031', N'TOUR010', 1, 1, N'DTV131', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 8, GETUTCDATE()), N'Tới Hà Giang, dạo phố cổ và chợ đêm'),
    (N'LT032', N'TOUR010', 2, 1, N'DTV132', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Đèo Mã Pí Lèng, sông Nho Quế'),
    (N'LT033', N'TOUR010', 3, 1, N'DTV133', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Nhà trình tường, cao nguyên đá'),
    (N'LT034', N'TOUR010', 4, 1, N'DTV135', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 3, GETUTCDATE()), N'Trả phòng, về Hà Nội'),
    (N'LT035', N'TOUR011', 1, 1, N'DTV201', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 9, GETUTCDATE()), N'Về Mai Châu, nhà sàn tối lửa'),
    (N'LT036', N'TOUR011', 2, 1, N'DTV202', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Pù Luông, ruộng bậc thang'),
    (N'LT037', N'TOUR011', 3, 1, N'DTV203', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Suối, về Hà Nội'),
    (N'LT038', N'TOUR012', 1, 1, N'DTV324', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 14, GETUTCDATE()), N'Nhận phòng, tắm biển Mỹ Khê'),
    (N'LT039', N'TOUR012', 2, 1, N'DTV322', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Cả ngày Bà Nà Hills'),
    (N'LT040', N'TOUR012', 3, 1, N'DTV321', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Sơn Trà, chùa Linh Ứng, tiễn sân bay'),
    (N'LT041', N'TOUR013', 1, 1, N'DTV351', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 14, GETUTCDATE()), N'Quy Nhơn, dạo biển Hoàng Hậu'),
    (N'LT042', N'TOUR013', 2, 1, N'DTV352', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Cano Kỳ Co - Eo Gió'),
    (N'LT043', N'TOUR013', 3, 1, N'DTV353', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Tháp Đôi, tiễn'),
    (N'LT044', N'TOUR014', 1, 1, N'DTV391', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 14, GETUTCDATE()), N'Nhận phòng Mũi Né, biển chiều'),
    (N'LT045', N'TOUR014', 2, 1, N'DTV392', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Đồi cát bay, suối Tiên'),
    (N'LT046', N'TOUR014', 3, 1, N'DTV395', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Hải sản, về'),
    (N'LT047', N'TOUR015', 1, 1, N'DTV481', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 8, GETUTCDATE()), N'Xe Sài Gòn - Vũng Tàu, Bãi Sau'),
    (N'LT048', N'TOUR015', 2, 1, N'DTV482', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Tượng Chúa, hải sản, về Sài Gòn'),
    (N'LT049', N'TOUR016', 1, 1, N'DTV491', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 7, GETUTCDATE()), N'Cáp treo Núi Bà Đen'),
    (N'LT050', N'TOUR016', 2, 1, N'DTV493', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Tòa Thánh, bánh tráng, về'),
    (N'LT051', N'TOUR017', 1, 1, N'DTV453', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 9, GETUTCDATE()), N'Dinh Độc Lập, Nhà thờ Đức Bà, Bến Thành'),
    (N'LT052', N'TOUR017', 2, 1, N'DTV451', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Địa đạo Củ Chi, phố đi bộ'),
    (N'LT053', N'TOUR018', 1, 1, N'DTV191', CAST(NULL AS nchar(20)), 1, 0, DATEADD(HOUR, 8, GETUTCDATE()), N'Lên Mộc Châu, đồi chè chiều'),
    (N'LT054', N'TOUR018', 2, 1, N'DTV192', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Thác Dải Yếm, rừng thông'),
    (N'LT055', N'TOUR018', 3, 1, N'DTV195', CAST(NULL AS nchar(20)), 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Chợ phiên, xuống núi')
) v(MaLichTrinh, MaTour, NgayThu, ThuTuTrongNgay, MaDThamQuan, MaSanPham, SoLuong, DonGia, ThoiGianDuKien, Mota)
WHERE EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
  AND EXISTS (SELECT 1 FROM dbo.DiemThamQuan d WHERE d.MaDThamQuan = v.MaDThamQuan)
  AND NOT EXISTS (SELECT 1 FROM dbo.LichTrinh x WHERE x.MaLichTrinh = v.MaLichTrinh);

INSERT INTO dbo.LichKhoiHanh (MaKhoiHanh, MaTour, NgayKhoiHanh, NgayKetThuc, DiaDiem, SoCho)
SELECT v.MaKhoiHanh, v.MaTour, v.NgayKhoiHanh, v.NgayKetThuc, v.DiaDiem, v.SoCho
FROM (VALUES
    (N'KH016', N'TOUR010', DATEADD(DAY, 21, GETUTCDATE()), DATEADD(DAY, 24, GETUTCDATE()), N'Hà Nội', 18),
    (N'KH017', N'TOUR010', DATEADD(DAY, 49, GETUTCDATE()), DATEADD(DAY, 52, GETUTCDATE()), N'Hà Nội', 18),
    (N'KH018', N'TOUR011', DATEADD(DAY, 14, GETUTCDATE()), DATEADD(DAY, 16, GETUTCDATE()), N'Hà Nội', 22),
    (N'KH019', N'TOUR011', DATEADD(DAY, 36, GETUTCDATE()), DATEADD(DAY, 38, GETUTCDATE()), N'Hà Nội', 22),
    (N'KH020', N'TOUR012', DATEADD(DAY, 11, GETUTCDATE()), DATEADD(DAY, 13, GETUTCDATE()), N'Đà Nẵng', 28),
    (N'KH021', N'TOUR012', DATEADD(DAY, 33, GETUTCDATE()), DATEADD(DAY, 35, GETUTCDATE()), N'Đà Nẵng', 28),
    (N'KH022', N'TOUR013', DATEADD(DAY, 17, GETUTCDATE()), DATEADD(DAY, 19, GETUTCDATE()), N'Quy Nhơn', 24),
    (N'KH023', N'TOUR013', DATEADD(DAY, 41, GETUTCDATE()), DATEADD(DAY, 43, GETUTCDATE()), N'Quy Nhơn', 24),
    (N'KH024', N'TOUR014', DATEADD(DAY, 13, GETUTCDATE()), DATEADD(DAY, 15, GETUTCDATE()), N'Phan Thiết', 26),
    (N'KH025', N'TOUR014', DATEADD(DAY, 40, GETUTCDATE()), DATEADD(DAY, 42, GETUTCDATE()), N'Phan Thiết', 26),
    (N'KH026', N'TOUR015', DATEADD(DAY, 8, GETUTCDATE()), DATEADD(DAY, 9, GETUTCDATE()), N'TP. Hồ Chí Minh', 32),
    (N'KH027', N'TOUR015', DATEADD(DAY, 22, GETUTCDATE()), DATEADD(DAY, 23, GETUTCDATE()), N'TP. Hồ Chí Minh', 32),
    (N'KH028', N'TOUR016', DATEADD(DAY, 9, GETUTCDATE()), DATEADD(DAY, 10, GETUTCDATE()), N'TP. Hồ Chí Minh', 30),
    (N'KH029', N'TOUR016', DATEADD(DAY, 30, GETUTCDATE()), DATEADD(DAY, 31, GETUTCDATE()), N'TP. Hồ Chí Minh', 30),
    (N'KH030', N'TOUR017', DATEADD(DAY, 7, GETUTCDATE()), DATEADD(DAY, 8, GETUTCDATE()), N'TP. Hồ Chí Minh', 28),
    (N'KH031', N'TOUR017', DATEADD(DAY, 28, GETUTCDATE()), DATEADD(DAY, 29, GETUTCDATE()), N'TP. Hồ Chí Minh', 28),
    (N'KH032', N'TOUR018', DATEADD(DAY, 19, GETUTCDATE()), DATEADD(DAY, 21, GETUTCDATE()), N'Hà Nội', 22),
    (N'KH033', N'TOUR018', DATEADD(DAY, 47, GETUTCDATE()), DATEADD(DAY, 49, GETUTCDATE()), N'Hà Nội', 22),
    (N'KH034', N'TOUR001', DATEADD(DAY, 70, GETUTCDATE()), DATEADD(DAY, 72, GETUTCDATE()), N'Hà Nội', 30),
    (N'KH035', N'TOUR002', DATEADD(DAY, 60, GETUTCDATE()), DATEADD(DAY, 62, GETUTCDATE()), N'Đà Nẵng', 25),
    (N'KH036', N'TOUR006', DATEADD(DAY, 55, GETUTCDATE()), DATEADD(DAY, 58, GETUTCDATE()), N'Nha Trang', 30),
    (N'KH037', N'TOUR009', DATEADD(DAY, 26, GETUTCDATE()), DATEADD(DAY, 28, GETUTCDATE()), N'Đà Lạt', 28)
) v(MaKhoiHanh, MaTour, NgayKhoiHanh, NgayKetThuc, DiaDiem, SoCho)
WHERE EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
  AND NOT EXISTS (SELECT 1 FROM dbo.LichKhoiHanh x WHERE x.MaKhoiHanh = v.MaKhoiHanh);

IF NOT EXISTS (SELECT 1 FROM dbo.NhomKhuyenMai WHERE MaNhomKM = N'NKM002')
    INSERT INTO dbo.NhomKhuyenMai (MaNhomKM, TenNhomKM) VALUES (N'NKM002', N'Ưu đãi theo mùa');

INSERT INTO dbo.KhuyenMai (MaKM, MaNhomKM, TenKM, MaCode, NgayBD, NgayKT, DonVi, GiamGia, CoCongDon, TrangThai)
SELECT v.MaKM, v.MaNhomKM, v.TenKM, v.MaCode, v.NgayBD, v.NgayKT, v.DonVi, v.GiamGia, v.CoCongDon, v.TrangThai
FROM (VALUES
    (N'KM004', N'NKM001', N'Chào khách mới', N'WELCOME15', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, 90, GETUTCDATE()), N'%', 15, 0, N'HoatDong'),
    (N'KM005', N'NKM002', N'Gia đình cuối tuần', N'FAMILY08', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, 60, GETUTCDATE()), N'%', 8, 0, N'HoatDong'),
    (N'KM006', N'NKM002', N'Đặt sớm giảm tiền mặt', N'EARLY500', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, 75, GETUTCDATE()), N'VND', 500000, 0, N'HoatDong'),
    (N'KM007', N'NKM002', N'Cao nguyên se lạnh mở rộng', N'HIGHLAND', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, 50, GETUTCDATE()), N'%', 10, 0, N'HoatDong')
) v(MaKM, MaNhomKM, TenKM, MaCode, NgayBD, NgayKT, DonVi, GiamGia, CoCongDon, TrangThai)
WHERE EXISTS (SELECT 1 FROM dbo.NhomKhuyenMai n WHERE n.MaNhomKM = v.MaNhomKM)
  AND NOT EXISTS (SELECT 1 FROM dbo.KhuyenMai x WHERE x.MaKM = v.MaKM);

INSERT INTO dbo.DieuKienKM (MaDK, MaKhuyenMai, DonToiThieu, LanDatDau, SoLuong)
SELECT v.MaDK, v.MaKhuyenMai, v.DonToiThieu, v.LanDatDau, v.SoLuong
FROM (VALUES
    (N'DK002', N'KM004', 2000000, CAST(1 AS bit), 200),
    (N'DK003', N'KM005', 4000000, CAST(0 AS bit), 80),
    (N'DK004', N'KM006', 5000000, CAST(0 AS bit), 50),
    (N'DK005', N'KM007', 3000000, CAST(0 AS bit), 100)
) v(MaDK, MaKhuyenMai, DonToiThieu, LanDatDau, SoLuong)
WHERE EXISTS (SELECT 1 FROM dbo.KhuyenMai k WHERE k.MaKM = v.MaKhuyenMai)
  AND NOT EXISTS (SELECT 1 FROM dbo.DieuKienKM x WHERE x.MaDK = v.MaDK);

IF COLUMNPROPERTY(OBJECT_ID(N'dbo.KM_Tour'), N'STT', 'IsIdentity') = 1
    INSERT INTO dbo.KM_Tour (MaKhuyenMai, MaTour)
    SELECT v.MaKhuyenMai, v.MaTour
    FROM (VALUES
        (N'KM002', N'TOUR007'),
        (N'KM003', N'TOUR009'),
        (N'KM004', N'TOUR001'),
        (N'KM004', N'TOUR002'),
        (N'KM005', N'TOUR006'),
        (N'KM005', N'TOUR012'),
        (N'KM005', N'TOUR015'),
        (N'KM006', N'TOUR010'),
        (N'KM006', N'TOUR007'),
        (N'KM007', N'TOUR003'),
        (N'KM007', N'TOUR018'),
        (N'KM007', N'TOUR011'),
        (N'KM001', N'TOUR004'),
        (N'KM001', N'TOUR005')
    ) v(MaKhuyenMai, MaTour)
    WHERE EXISTS (SELECT 1 FROM dbo.KhuyenMai k WHERE k.MaKM = v.MaKhuyenMai)
      AND EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.KM_Tour x
          WHERE x.MaKhuyenMai = v.MaKhuyenMai AND x.MaTour = v.MaTour);
ELSE
    INSERT INTO dbo.KM_Tour (STT, MaKhuyenMai, MaTour)
    SELECT ISNULL((SELECT MAX(STT) FROM dbo.KM_Tour), 0)
           + ROW_NUMBER() OVER (ORDER BY v.MaKhuyenMai, v.MaTour),
           v.MaKhuyenMai, v.MaTour
    FROM (VALUES
        (N'KM002', N'TOUR007'),
        (N'KM003', N'TOUR009'),
        (N'KM004', N'TOUR001'),
        (N'KM004', N'TOUR002'),
        (N'KM005', N'TOUR006'),
        (N'KM005', N'TOUR012'),
        (N'KM005', N'TOUR015'),
        (N'KM006', N'TOUR010'),
        (N'KM006', N'TOUR007'),
        (N'KM007', N'TOUR003'),
        (N'KM007', N'TOUR018'),
        (N'KM007', N'TOUR011'),
        (N'KM001', N'TOUR004'),
        (N'KM001', N'TOUR005')
    ) v(MaKhuyenMai, MaTour)
    WHERE EXISTS (SELECT 1 FROM dbo.KhuyenMai k WHERE k.MaKM = v.MaKhuyenMai)
      AND EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.KM_Tour x
          WHERE x.MaKhuyenMai = v.MaKhuyenMai AND x.MaTour = v.MaTour);

INSERT INTO dbo.DanhGiaTour (MaDanhGiaTour, MaUser, MaTour, ThoiGian, SaoDanhGia, NhanXet)
SELECT v.MaDanhGiaTour, v.MaUser, v.MaTour, v.ThoiGian, v.SaoDanhGia, v.NhanXet
FROM (VALUES
    (N'DG007', N'USRKH000001', N'TOUR006', DATEADD(DAY, -18, GETUTCDATE()), 5, N'Biển Nha Trang đẹp, khách sạn gần bãi. VinWonders đông nhưng vui.'),
    (N'DG008', N'USRKH000002', N'TOUR009', DATEADD(DAY, -10, GETUTCDATE()), 4, N'Đà Lạt se lạnh đúng hẹn. Đồi chè Cầu Đất nên đi sớm.'),
    (N'DG009', N'USRKH000001', N'TOUR005', DATEADD(DAY, -25, GETUTCDATE()), 5, N'Huế chậm và đáng. Thuyền sông Hương lúc hoàng hôn rất yên.'),
    (N'DG010', N'USRKH000002', N'TOUR008', DATEADD(DAY, -7, GETUTCDATE()), 5, N'Chợ nổi Cái Răng phải đi lúc sáng. Ăn hủ tiếu trên ghe đáng nhớ.')
) v(MaDanhGiaTour, MaUser, MaTour, ThoiGian, SaoDanhGia, NhanXet)
WHERE EXISTS (SELECT 1 FROM dbo.NguoiSuDung u WHERE u.MaUser = v.MaUser)
  AND EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
  AND NOT EXISTS (SELECT 1 FROM dbo.DanhGiaTour x WHERE x.MaDanhGiaTour = v.MaDanhGiaTour);

INSERT INTO dbo.HuongDanVien (MaHuongDanVien, HoTen, NgaySinh, QueQuan, Email, CCCD, SoDienThoai)
SELECT v.MaHuongDanVien, v.HoTen, v.NgaySinh, v.QueQuan, v.Email, v.CCCD, v.SoDienThoai
FROM (VALUES
    (N'HDV003', N'Lê Minh Châu', CAST('1991-03-12' AS date), N'Hà Giang', N'hdv3@example.com', N'001091000003', N'0912000003'),
    (N'HDV004', N'Phạm Quốc Huy', CAST('1988-11-02' AS date), N'Đà Nẵng', N'hdv4@example.com', N'001088000004', N'0912000004')
) v(MaHuongDanVien, HoTen, NgaySinh, QueQuan, Email, CCCD, SoDienThoai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.HuongDanVien x WHERE x.MaHuongDanVien = v.MaHuongDanVien);

INSERT INTO dbo.LichDanTour (MaLichDanTour, MaHDV, MaTour, MaKhoiHanh)
SELECT v.MaLichDanTour, v.MaHDV, v.MaTour, v.MaKhoiHanh
FROM (VALUES
    (N'LDT003', N'HDV003', N'TOUR010', N'KH016'),
    (N'LDT004', N'HDV004', N'TOUR012', N'KH020'),
    (N'LDT005', N'HDV001', N'TOUR011', N'KH018'),
    (N'LDT006', N'HDV002', N'TOUR017', N'KH030')
) v(MaLichDanTour, MaHDV, MaTour, MaKhoiHanh)
WHERE EXISTS (SELECT 1 FROM dbo.HuongDanVien h WHERE h.MaHuongDanVien = v.MaHDV)
  AND EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour)
  AND EXISTS (SELECT 1 FROM dbo.LichKhoiHanh k WHERE k.MaKhoiHanh = v.MaKhoiHanh)
  AND NOT EXISTS (SELECT 1 FROM dbo.LichDanTour x WHERE x.MaLichDanTour = v.MaLichDanTour);
GO
