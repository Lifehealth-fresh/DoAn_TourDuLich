/* Sửa lỗi UTF-8 do sqlcmd seed sai encoding. Chạy bằng PowerShell SqlClient. */

UPDATE dbo.KhuVuc SET TenKhuVuc = N'Miền Bắc', QuocGia = N'Việt Nam' WHERE MaKhuVuc = N'KV001';
UPDATE dbo.KhuVuc SET TenKhuVuc = N'Miền Trung', QuocGia = N'Việt Nam' WHERE MaKhuVuc = N'KV002';
UPDATE dbo.KhuVuc SET TenKhuVuc = N'Miền Nam', QuocGia = N'Việt Nam' WHERE MaKhuVuc = N'KV003';

UPDATE dbo.DiemThamQuan SET TenDiaDanh = N'Hồ Hoàn Kiếm', DiaChi = N'Đinh Tiên Hoàng, Hà Nội', Mota = N'Khu vực trung tâm Hà Nội' WHERE MaDThamQuan = N'DT001';
UPDATE dbo.DiemThamQuan SET TenDiaDanh = N'Vịnh Hạ Long', DiaChi = N'Thành phố Hạ Long, Quảng Ninh', Mota = N'Di sản thiên nhiên thế giới' WHERE MaDThamQuan = N'DT002';
UPDATE dbo.DiemThamQuan SET TenDiaDanh = N'Phố cổ Hội An', DiaChi = N'Thành phố Hội An, Quảng Nam', Mota = N'Khu phố cổ lịch sử' WHERE MaDThamQuan = N'DT003';
UPDATE dbo.DiemThamQuan SET TenDiaDanh = N'Bảo tàng Chăm', DiaChi = N'Đà Nẵng', Mota = N'Bảo tàng điêu khắc Chăm' WHERE MaDThamQuan = N'DT004';

UPDATE dbo.DoiTac SET TenDoiTac = N'Khách sạn Hoàn Kiếm', NguoiLienHe = N'Nguyễn An' WHERE MaDoiTac = N'DTAC001';
UPDATE dbo.DoiTac SET TenDoiTac = N'Vận tải Miền Trung', NguoiLienHe = N'Trần Bình' WHERE MaDoiTac = N'DTAC002';
UPDATE dbo.DoiTac SET TenDoiTac = N'Ẩm thực Phố Hội', NguoiLienHe = N'Lê Chi' WHERE MaDoiTac = N'DTAC003';
UPDATE dbo.DoiTac SET TenDoiTac = N'Trải nghiệm Việt', NguoiLienHe = N'Phạm Dũng' WHERE MaDoiTac = N'DTAC004';

UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Phòng tiêu chuẩn', DonViTinh = N'phòng', Mota = N'Phòng nghỉ trung tâm' WHERE MaSanPham = N'SP001';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Phòng hướng hồ', DonViTinh = N'phòng', Mota = N'Phòng nghỉ cao cấp' WHERE MaSanPham = N'SP002';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Xe đưa đón sân bay', DonViTinh = N'chuyến', Mota = N'Dịch vụ xe riêng' WHERE MaSanPham = N'SP003';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Xe tham quan 16 chỗ', DonViTinh = N'ngày', Mota = N'Xe du lịch' WHERE MaSanPham = N'SP004';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Bữa tối đặc sản Hội An', DonViTinh = N'suất', Mota = N'Thực đơn địa phương' WHERE MaSanPham = N'SP005';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Lớp học nấu ăn', DonViTinh = N'người', Mota = N'Trải nghiệm ẩm thực' WHERE MaSanPham = N'SP006';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Tour thuyền Vịnh Hạ Long', DonViTinh = N'người', Mota = N'Tham quan bằng thuyền' WHERE MaSanPham = N'SP007';
UPDATE dbo.SanPhamDoiTac SET TenSanPham = N'Vé tham quan bảo tàng', DonViTinh = N'vé', Mota = N'Vé vào cửa' WHERE MaSanPham = N'SP008';

UPDATE dbo.Tour SET TenTour = N'Hà Nội - Hạ Long', Mota = N'Hành trình miền Bắc tiêu biểu', DieuKhoan = N'Tuân thủ lịch trình của đoàn.' WHERE MaTour = N'TOUR001';
UPDATE dbo.Tour SET TenTour = N'Đà Nẵng - Hội An', Mota = N'Hành trình di sản miền Trung', DieuKhoan = N'Tuân thủ lịch trình của đoàn.' WHERE MaTour = N'TOUR002';

UPDATE dbo.LichTrinh SET Mota = N'Tham quan Hồ Hoàn Kiếm' WHERE MaLichTrinh = N'LT001';
UPDATE dbo.LichTrinh SET Mota = N'Tham quan Vịnh Hạ Long' WHERE MaLichTrinh = N'LT002';
UPDATE dbo.LichTrinh SET Mota = N'Tham quan Phố cổ Hội An' WHERE MaLichTrinh = N'LT003';
UPDATE dbo.LichTrinh SET Mota = N'Tham quan Bảo tàng Chăm' WHERE MaLichTrinh = N'LT004';

UPDATE dbo.LichKhoiHanh SET DiaDiem = N'Hà Nội' WHERE MaKhoiHanh = N'KH001';
UPDATE dbo.LichKhoiHanh SET DiaDiem = N'Đà Nẵng' WHERE MaKhoiHanh = N'KH002';

UPDATE dbo.HuongDanVien SET HoTen = N'Nguyễn Hướng Dẫn', QueQuan = N'Hà Nội' WHERE MaHuongDanVien = N'HDV001';
UPDATE dbo.HuongDanVien SET HoTen = N'Trần Du Lịch', QueQuan = N'Đà Nẵng' WHERE MaHuongDanVien = N'HDV002';

UPDATE dbo.NhomKhuyenMai SET TenNhomKM = N'Khuyến mãi khách mới' WHERE MaNhomKM = N'NKM001';
UPDATE dbo.KhuyenMai SET TenKM = N'Giảm giá mùa hè' WHERE MaKM = N'KM001';

UPDATE dbo.KhachHang SET Ho = N'Nguyễn', Ten = N'An', HoGiayTo = N'Nguyễn', TenGiayTo = N'An', QuocTich = N'Việt Nam' WHERE MaKhachHang = N'KHACH001';
UPDATE dbo.KhachHang SET Ho = N'Trần', Ten = N'Bình', HoGiayTo = N'Trần', TenGiayTo = N'Bình', QuocTich = N'Việt Nam' WHERE MaKhachHang = N'KHACH002';

UPDATE dbo.GiayTo SET NoiCap = N'Cục CSQLHC' WHERE MaGiayTo IN (N'GT001', N'GT002');

UPDATE dbo.AIGoiY SET LyDo = N'Phù hợp sở thích tham quan miền Bắc' WHERE MaRecommodation = N'GOIY000001';
