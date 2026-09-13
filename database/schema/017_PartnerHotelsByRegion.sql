/* Khách sạn theo khu vực phục vụ tự thiết kế. Không sửa 001. */

UPDATE dbo.SanPhamDoiTac
SET DonViTinh = N'dem'
WHERE MaDoiTac IN (SELECT MaDoiTac FROM dbo.DoiTac WHERE RTRIM(LoaiDoiTac) = N'LuuTru')
  AND (DonViTinh IS NULL OR RTRIM(DonViTinh) <> N'dem');

IF NOT EXISTS (SELECT 1 FROM dbo.DiemThamQuan WHERE MaDThamQuan = N'DT005')
    INSERT INTO dbo.DiemThamQuan (MaDThamQuan, TenDiaDanh, DiaChi, MaKhuVuc, KinhDo, ViDo, Mota)
    VALUES (N'DT005', N'Chợ Bến Thành', N'Quận 1, TP.HCM', N'KV003', CAST(106.698300 AS decimal(9,6)), CAST(10.772500 AS decimal(9,6)), N'Chợ trung tâm Sài Gòn');

IF NOT EXISTS (SELECT 1 FROM dbo.DoiTac WHERE MaDoiTac = N'DTAC005')
    INSERT INTO dbo.DoiTac (MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, MaKhuVuc, PhanTramHoaHong, TrangThai)
    VALUES (N'DTAC005', N'Khách sạn Phố Hội', N'LuuTru', N'Ngô Lan', N'0901000005', N'hoianks@example.com', N'KV002', CAST(10.00 AS decimal(5,2)), N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.DoiTac WHERE MaDoiTac = N'DTAC006')
    INSERT INTO dbo.DoiTac (MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, MaKhuVuc, PhanTramHoaHong, TrangThai)
    VALUES (N'DTAC006', N'Khách sạn Bến Thành', N'LuuTru', N'Vũ Minh', N'0901000006', N'saigonks@example.com', N'KV003', CAST(10.00 AS decimal(5,2)), N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.SanPhamDoiTac WHERE MaSanPham = N'SP009')
    INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
    VALUES (N'SP009', N'DTAC005', N'Phòng Deluxe', N'dem', 1500000, NULL, N'Phòng 2 giường, gồm ăn sáng', N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.SanPhamDoiTac WHERE MaSanPham = N'SP010')
    INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
    VALUES (N'SP010', N'DTAC005', N'Phòng tiêu chuẩn', N'dem', 900000, NULL, N'Phòng 2 người', N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.SanPhamDoiTac WHERE MaSanPham = N'SP011')
    INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
    VALUES (N'SP011', N'DTAC006', N'Phòng Deluxe', N'dem', 1800000, NULL, N'Phòng view trung tâm', N'HoatDong');

IF NOT EXISTS (SELECT 1 FROM dbo.SanPhamDoiTac WHERE MaSanPham = N'SP012')
    INSERT INTO dbo.SanPhamDoiTac (MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai)
    VALUES (N'SP012', N'DTAC006', N'Phòng tiêu chuẩn', N'dem', 1100000, NULL, N'Phòng 2 người', N'HoatDong');
