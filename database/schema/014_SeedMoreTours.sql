/* Bổ sung địa điểm, tour, lịch trình, khởi hành, đánh giá. Chạy bằng PowerShell SqlClient (UTF-8). */

UPDATE dbo.Tour SET
  Mota = N'Ba ngày Hà Nội - Hạ Long: phố cổ, ẩm thực đêm, rồi ngủ đêm trên vịnh di sản. Nhịp chậm, nhiều khoảng lặng nhìn đá và nước.',
  DieuKhoan = N'Giá đã gồm khách sạn/du thuyền, xe, bữa chính theo lịch, vé thắng cảnh và hướng dẫn viên. Không gồm vé máy bay và chi tiêu cá nhân.'
WHERE MaTour = N'TOUR001';

UPDATE dbo.Tour SET
  Mota = N'Đà Nẵng - Hội An ba ngày: biển Mỹ Khê, Ngũ Hành Sơn, phố đèn lồng và lớp nấu ăn. Phù hợp cặp đôi và nhóm bạn thích văn hóa miền Trung.',
  DieuKhoan = N'Giá gồm khách sạn, xe đưa đón, bữa chính, vé điểm đến trong lịch. Không gồm vé máy bay.'
WHERE MaTour = N'TOUR002';

IF NOT EXISTS (SELECT 1 FROM dbo.DiemThamQuan WHERE MaDThamQuan = N'DT005')
INSERT INTO dbo.DiemThamQuan (MaDThamQuan, TenDiaDanh, DiaChi, MaKhuVuc, KinhDo, ViDo, Mota) VALUES
(N'DT005', N'Thị trấn Sa Pa', N'Sa Pa, Lào Cai', N'KV001', 103.844800, 22.336400, N'Thị trấn sương và ruộng bậc thang'),
(N'DT006', N'Fansipan', N'Sa Pa, Lào Cai', N'KV001', 103.775000, 22.303300, N'Nóc nhà Đông Dương'),
(N'DT007', N'Trang An', N'Ninh Bình', N'KV001', 105.916700, 20.250000, N'Danh thắng sông núi'),
(N'DT008', N'Tam Cốc', N'Hoa Lư, Ninh Bình', N'KV001', 105.933300, 20.216700, N'Hang động và lúa nước'),
(N'DT009', N'Đại Nội Huế', N'Thành phố Huế', N'KV002', 107.577900, 16.469800, N'Kinh thành triều Nguyễn'),
(N'DT010', N'Sông Hương', N'Thành phố Huế', N'KV002', 107.590000, 16.466700, N'Du thuyền hoàng hôn'),
(N'DT011', N'Biển Nha Trang', N'Khánh Hòa', N'KV002', 109.196700, 12.238800, N'Bãi biển vịnh'),
(N'DT012', N'VinWonders Nha Trang', N'Hòn Tre, Khánh Hòa', N'KV002', 109.278000, 12.216000, N'Công viên đảo'),
(N'DT013', N'Bãi Sao', N'Phú Quốc, Kiên Giang', N'KV003', 103.973000, 10.033000, N'Bãi cát trắng'),
(N'DT014', N'Chợ đêm Phú Quốc', N'Dương Đông, Phú Quốc', N'KV003', 103.967000, 10.217000, N'Hải sản và đêm đảo'),
(N'DT015', N'Chợ nổi Cái Răng', N'Cần Thơ', N'KV003', 105.787000, 10.007000, N'Chợ nổi sông Hậu'),
(N'DT016', N'Vườn trái cây Phong Điền', N'Cần Thơ', N'KV003', 105.670000, 9.990000, N'Vườn miền Tây'),
(N'DT017', N'Đồi chè Cầu Đất', N'Đà Lạt, Lâm Đồng', N'KV003', 108.550000, 11.850000, N'Đồi chè cao nguyên'),
(N'DT018', N'Hồ Xuân Hương', N'Trung tâm Đà Lạt', N'KV003', 108.441900, 11.940400, N'Hồ giữa thành phố ngàn hoa');

IF NOT EXISTS (SELECT 1 FROM dbo.Tour WHERE MaTour = N'TOUR003')
INSERT INTO dbo.Tour (MaTour, TenTour, Mota, ThoiGian, DieuKhoan, GiaTour, SLKhach, SLHuongDanVien, LoaiTour, TrangThai) VALUES
(N'TOUR003', N'Sa Pa - Fansipan', N'Bốn ngày Tây Bắc: bản Cát Cát, cáp treo Fansipan, chợ tình và ruộng bậc thang. Trời se lạnh, đồ len, bát thắng cố.', 4, N'Gồm khách sạn, xe giường nằm, bữa chính, vé cáp treo. Nên mang áo ấm.', 5200000, 25, 1, N'Chuan', N'HoatDong'),
(N'TOUR004', N'Ninh Bình - Tràng An', N'Ba ngày cố đô: Tràng An, Tam Cốc, Bái Đính, cơm cháy. Sông lặng, núi đá vôi, chiều vàng trên đồng.', 3, N'Gồm khách sạn, xe, thuyền, vé danh thắng, bữa chính.', 2900000, 28, 1, N'Chuan', N'HoatDong'),
(N'TOUR005', N'Huế - Sông Hương', N'Ba ngày cố đô: Đại Nội, lăng tẩm, thuyền sông Hương, ẩm thực cung đình. Chậm, trang trọng, nhiều câu chuyện.', 3, N'Gồm khách sạn, xe, vé Đại Nội, thuyền chiều, bữa chính.', 3100000, 24, 1, N'Chuan', N'HoatDong'),
(N'TOUR006', N'Nha Trang vịnh biển', N'Bốn ngày nắng: tắm biển, đảo Hòn Tre, hải sản đêm. Nhóm bạn và gia đình có trẻ.', 4, N'Gồm khách sạn gần biển, xe, vé VinWonders, bữa chính.', 4800000, 30, 1, N'Chuan', N'HoatDong'),
(N'TOUR007', N'Phú Quốc - Bãi Sao', N'Bốn ngày đảo ngọc: Bãi Sao, sunset Sanato, chợ đêm Dương Đông. Cát mịn, nước trong.', 4, N'Gồm khách sạn, xe đưa đón sân bay, xe tham quan, bữa chính. Vé máy bay tự túc.', 5600000, 22, 1, N'Chuan', N'HoatDong'),
(N'TOUR008', N'Cần Thơ - Chợ nổi', N'Ba ngày miền Tây: chợ nổi Cái Răng, vườn trái, đờn ca tài tử. Sông nước, vị ngọt.', 3, N'Gồm khách sạn, thuyền chợ nổi, vườn trái, bữa chính.', 2700000, 26, 1, N'Chuan', N'HoatDong'),
(N'TOUR009', N'Đà Lạt ngàn hoa', N'Ba ngày cao nguyên: hồ Xuân Hương, đồi chè Cầu Đất, đêm sương. Mát quanh năm.', 3, N'Gồm khách sạn trung tâm, xe, điểm check-in, bữa chính.', 3300000, 28, 1, N'Chuan', N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.LichTrinh WHERE MaLichTrinh = N'LT005')
INSERT INTO dbo.LichTrinh (MaLichTrinh, MaTour, NgayThu, ThuTuTrongNgay, MaDThamQuan, MaSanPham, SoLuong, DonGia, ThoiGianDuKien, Mota) VALUES
(N'LT005', N'TOUR001', 3, 1, N'DT002', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Thưởng thức bình minh trên vịnh rồi về Hà Nội'),
(N'LT006', N'TOUR002', 3, 1, N'DT003', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Dạo đèn lồng Hội An, thả hoa đăng'),
(N'LT007', N'TOUR003', 1, 1, N'DT005', NULL, 1, 0, DATEADD(HOUR, 8, GETUTCDATE()), N'Lên Sa Pa, dạo thị trấn sương'),
(N'LT008', N'TOUR003', 2, 1, N'DT006', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Cáp treo Fansipan, chinh phục nóc nhà Đông Dương'),
(N'LT009', N'TOUR003', 3, 1, N'DT005', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Bản Cát Cát, ruộng bậc thang'),
(N'LT010', N'TOUR003', 4, 1, N'DT005', NULL, 1, 0, DATEADD(DAY, 3, GETUTCDATE()), N'Chợ phiên rồi xuống núi'),
(N'LT011', N'TOUR004', 1, 1, N'DT007', NULL, 1, 0, DATEADD(HOUR, 9, GETUTCDATE()), N'Thuyền Tràng An xuyên hang'),
(N'LT012', N'TOUR004', 2, 1, N'DT008', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Tam Cốc mùa lúa, chùa Bái Đính'),
(N'LT013', N'TOUR004', 3, 1, N'DT007', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Cố đô Hoa Lư, về Hà Nội'),
(N'LT014', N'TOUR005', 1, 1, N'DT009', NULL, 1, 0, DATEADD(HOUR, 9, GETUTCDATE()), N'Tham quan Đại Nội, Ngọ Môn'),
(N'LT015', N'TOUR005', 2, 1, N'DT010', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Thuyền sông Hương lúc hoàng hôn'),
(N'LT016', N'TOUR005', 3, 1, N'DT009', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Lăng Tự Đức, ẩm thực cung đình'),
(N'LT017', N'TOUR006', 1, 1, N'DT011', NULL, 1, 0, DATEADD(HOUR, 10, GETUTCDATE()), N'Nhận phòng, tắm biển Trần Phú'),
(N'LT018', N'TOUR006', 2, 1, N'DT012', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Cả ngày VinWonders Hòn Tre'),
(N'LT019', N'TOUR006', 3, 1, N'DT011', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Lặn biển / cano vịnh, hải sản đêm'),
(N'LT020', N'TOUR006', 4, 1, N'DT011', NULL, 1, 0, DATEADD(DAY, 3, GETUTCDATE()), N'Tự do mua sắm rồi ra sân bay'),
(N'LT021', N'TOUR007', 1, 1, N'DT014', NULL, 1, 0, DATEADD(HOUR, 14, GETUTCDATE()), N'Đón sân bay, chợ đêm Dương Đông'),
(N'LT022', N'TOUR007', 2, 1, N'DT013', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Cả ngày Bãi Sao, hoàng hôn Sanato'),
(N'LT023', N'TOUR007', 3, 1, N'DT013', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Câu cá / Grand World, nghỉ dưỡng'),
(N'LT024', N'TOUR007', 4, 1, N'DT014', NULL, 1, 0, DATEADD(DAY, 3, GETUTCDATE()), N'Tiễn sân bay'),
(N'LT025', N'TOUR008', 1, 1, N'DT015', NULL, 1, 0, DATEADD(HOUR, 5, GETUTCDATE()), N'Chợ nổi Cái Răng lúc sáng sớm'),
(N'LT026', N'TOUR008', 2, 1, N'DT016', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Vườn trái Phong Điền, đờn ca tài tử'),
(N'LT027', N'TOUR008', 3, 1, N'DT015', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Bến Ninh Kiều, về Sài Gòn'),
(N'LT028', N'TOUR009', 1, 1, N'DT018', NULL, 1, 0, DATEADD(HOUR, 10, GETUTCDATE()), N'Hồ Xuân Hương, chợ Đà Lạt'),
(N'LT029', N'TOUR009', 2, 1, N'DT017', NULL, 1, 0, DATEADD(DAY, 1, GETUTCDATE()), N'Đồi chè Cầu Đất, Langbiang'),
(N'LT030', N'TOUR009', 3, 1, N'DT018', NULL, 1, 0, DATEADD(DAY, 2, GETUTCDATE()), N'Thiền viện Trúc Lâm, về');

IF NOT EXISTS (SELECT 1 FROM dbo.LichKhoiHanh WHERE MaKhoiHanh = N'KH003')
INSERT INTO dbo.LichKhoiHanh (MaKhoiHanh, MaTour, NgayKhoiHanh, NgayKetThuc, DiaDiem) VALUES
(N'KH003', N'TOUR001', DATEADD(DAY, 45, GETUTCDATE()), DATEADD(DAY, 47, GETUTCDATE()), N'Hà Nội'),
(N'KH004', N'TOUR002', DATEADD(DAY, 20, GETUTCDATE()), DATEADD(DAY, 22, GETUTCDATE()), N'Đà Nẵng'),
(N'KH005', N'TOUR003', DATEADD(DAY, 18, GETUTCDATE()), DATEADD(DAY, 21, GETUTCDATE()), N'Hà Nội'),
(N'KH006', N'TOUR003', DATEADD(DAY, 40, GETUTCDATE()), DATEADD(DAY, 43, GETUTCDATE()), N'Hà Nội'),
(N'KH007', N'TOUR004', DATEADD(DAY, 14, GETUTCDATE()), DATEADD(DAY, 16, GETUTCDATE()), N'Hà Nội'),
(N'KH008', N'TOUR004', DATEADD(DAY, 35, GETUTCDATE()), DATEADD(DAY, 37, GETUTCDATE()), N'Hà Nội'),
(N'KH009', N'TOUR005', DATEADD(DAY, 16, GETUTCDATE()), DATEADD(DAY, 18, GETUTCDATE()), N'Huế'),
(N'KH010', N'TOUR006', DATEADD(DAY, 22, GETUTCDATE()), DATEADD(DAY, 25, GETUTCDATE()), N'Nha Trang'),
(N'KH011', N'TOUR007', DATEADD(DAY, 12, GETUTCDATE()), DATEADD(DAY, 15, GETUTCDATE()), N'Phú Quốc'),
(N'KH012', N'TOUR007', DATEADD(DAY, 38, GETUTCDATE()), DATEADD(DAY, 41, GETUTCDATE()), N'Phú Quốc'),
(N'KH013', N'TOUR008', DATEADD(DAY, 10, GETUTCDATE()), DATEADD(DAY, 12, GETUTCDATE()), N'Cần Thơ'),
(N'KH014', N'TOUR009', DATEADD(DAY, 19, GETUTCDATE()), DATEADD(DAY, 21, GETUTCDATE()), N'Đà Lạt'),
(N'KH015', N'TOUR009', DATEADD(DAY, 42, GETUTCDATE()), DATEADD(DAY, 44, GETUTCDATE()), N'Đà Lạt');

IF NOT EXISTS (SELECT 1 FROM dbo.DanhGiaTour WHERE MaDanhGiaTour = N'DG001')
INSERT INTO dbo.DanhGiaTour (MaDanhGiaTour, MaUser, MaTour, ThoiGian, SaoDanhGia, NhanXet) VALUES
(N'DG001', N'USRKH000001', N'TOUR001', DATEADD(DAY, -20, GETUTCDATE()), 5, N'Vịnh đẹp hơn ảnh. Du thuyền sạch, bình minh ngày 3 đáng để dậy sớm.'),
(N'DG002', N'USRKH000002', N'TOUR001', DATEADD(DAY, -12, GETUTCDATE()), 4, N'Hà Nội hơi gấp ngày đầu, nhưng Hạ Long bù lại rất đã.'),
(N'DG003', N'USRKH000001', N'TOUR002', DATEADD(DAY, -8, GETUTCDATE()), 5, N'Hội An về đêm như một tấm postcard. Ăn mì Quảng đúng ý.'),
(N'DG004', N'USRKH000002', N'TOUR003', DATEADD(DAY, -30, GETUTCDATE()), 5, N'Fansipan mây phủ, ruộng bậc thang sau mưa. Nên mang áo ấm.'),
(N'DG005', N'USRKH000001', N'TOUR004', DATEADD(DAY, -15, GETUTCDATE()), 5, N'Thuyền Tràng An yên, nước trong. Phù hợp gia đình.'),
(N'DG006', N'USRKH000002', N'TOUR007', DATEADD(DAY, -6, GETUTCDATE()), 4, N'Bãi Sao đẹp nhất đảo. Sunset Sanato đông nhưng đáng.');

IF NOT EXISTS (SELECT 1 FROM dbo.KhuyenMai WHERE MaKM = N'KM002')
INSERT INTO dbo.KhuyenMai (MaKM, MaNhomKM, TenKM, MaCode, NgayBD, NgayKT, DonVi, GiamGia, CoCongDon, TrangThai)
VALUES (N'KM002', N'NKM001', N'Ưu đãi đảo ngọc', N'ISLAND10', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, 60, GETUTCDATE()), N'%', 12, 0, N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.KhuyenMai WHERE MaKM = N'KM003')
INSERT INTO dbo.KhuyenMai (MaKM, MaNhomKM, TenKM, MaCode, NgayBD, NgayKT, DonVi, GiamGia, CoCongDon, TrangThai)
VALUES (N'KM003', N'NKM001', N'Cao nguyên se lạnh', N'DALAT08', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, 45, GETUTCDATE()), N'%', 8, 0, N'HoatDong');
