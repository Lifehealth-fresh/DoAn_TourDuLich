-- 024: khớp mọi tỉnh không cần Google Maps.
-- Alias đầy đủ + backfill MaTinh còn thiếu. Thời gian đi mọi cặp tỉnh do C# (Haversine).
-- Idempotent. Chạy sau 018 + 023. Không xóa user/booking.

IF OBJECT_ID(N'dbo.TinhThanhAlias', N'U') IS NULL
    THROW 50001, N'Chạy 023_TuThietKeChatPlanner.sql trước 024.', 1;
GO

IF OBJECT_ID(N'dbo.MatranDiChuyen', N'U') IS NOT NULL
AND EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Matran_PhuongTien')
    ALTER TABLE dbo.MatranDiChuyen DROP CONSTRAINT CK_Matran_PhuongTien;
GO
IF OBJECT_ID(N'dbo.MatranDiChuyen', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Matran_PhuongTien')
    ALTER TABLE dbo.MatranDiChuyen WITH NOCHECK
    ADD CONSTRAINT CK_Matran_PhuongTien CHECK (RTRIM(PhuongTien) IN (N'MayBay', N'Tau', N'XeKhach', N'XeMay'));
GO

MERGE dbo.TinhThanhAlias AS t
USING (VALUES
    (N'TN01                ', N'Hà Nội'),
    (N'TN01                ', N'Ha Noi'),
    (N'TN01                ', N'Hanoi'),
    (N'TN01                ', N'Thủ đô'),
    (N'TN01                ', N'Thu do'),
    (N'TN02                ', N'Hải Phòng'),
    (N'TN02                ', N'Hai Phong'),
    (N'TN02                ', N'Haiphong'),
    (N'TN02                ', N'Cát Bà'),
    (N'TN02                ', N'Cat Ba'),
    (N'TN02                ', N'Đồ Sơn'),
    (N'TN02                ', N'Do Son'),
    (N'TN03                ', N'Quảng Ninh'),
    (N'TN03                ', N'Quang Ninh'),
    (N'TN03                ', N'Hạ Long'),
    (N'TN03                ', N'Ha Long'),
    (N'TN03                ', N'Vịnh Hạ Long'),
    (N'TN03                ', N'Bai Chay'),
    (N'TN03                ', N'Bãi Cháy'),
    (N'TN03                ', N'Yên Tử'),
    (N'TN03                ', N'Yen Tu'),
    (N'TN04                ', N'Bắc Ninh'),
    (N'TN04                ', N'Bac Ninh'),
    (N'TN05                ', N'Hải Dương'),
    (N'TN05                ', N'Hai Duong'),
    (N'TN06                ', N'Hưng Yên'),
    (N'TN06                ', N'Hung Yen'),
    (N'TN07                ', N'Vĩnh Phúc'),
    (N'TN07                ', N'Vinh Phuc'),
    (N'TN07                ', N'Vĩnh Yên'),
    (N'TN07                ', N'Vinh Yen'),
    (N'TN08                ', N'Thái Nguyên'),
    (N'TN08                ', N'Thai Nguyen'),
    (N'TN09                ', N'Phú Thọ'),
    (N'TN09                ', N'Phu Tho'),
    (N'TN09                ', N'Việt Trì'),
    (N'TN09                ', N'Viet Tri'),
    (N'TN10                ', N'Bắc Giang'),
    (N'TN10                ', N'Bac Giang'),
    (N'TN11                ', N'Lạng Sơn'),
    (N'TN11                ', N'Lang Son'),
    (N'TN12                ', N'Cao Bằng'),
    (N'TN12                ', N'Cao Bang'),
    (N'TN13                ', N'Hà Giang'),
    (N'TN13                ', N'Ha Giang'),
    (N'TN13                ', N'Đồng Văn'),
    (N'TN13                ', N'Dong Van'),
    (N'TN14                ', N'Tuyên Quang'),
    (N'TN14                ', N'Tuyen Quang'),
    (N'TN15                ', N'Lào Cai'),
    (N'TN15                ', N'Lao Cai'),
    (N'TN15                ', N'Sa Pa'),
    (N'TN15                ', N'Sapa'),
    (N'TN15                ', N'Fansipan'),
    (N'TN16                ', N'Yên Bái'),
    (N'TN16                ', N'Yen Bai'),
    (N'TN17                ', N'Điện Biên'),
    (N'TN17                ', N'Dien Bien'),
    (N'TN17                ', N'Điện Biên Phủ'),
    (N'TN17                ', N'Dien Bien Phu'),
    (N'TN18                ', N'Lai Châu'),
    (N'TN18                ', N'Lai Chau'),
    (N'TN19                ', N'Sơn La'),
    (N'TN19                ', N'Son La'),
    (N'TN20                ', N'Hòa Bình'),
    (N'TN20                ', N'Hoa Binh'),
    (N'TN20                ', N'Mai Châu'),
    (N'TN20                ', N'Mai Chau'),
    (N'TN21                ', N'Ninh Bình'),
    (N'TN21                ', N'Ninh Binh'),
    (N'TN21                ', N'Tràng An'),
    (N'TN21                ', N'Trang An'),
    (N'TN21                ', N'Tam Cốc'),
    (N'TN21                ', N'Tam Coc'),
    (N'TN21                ', N'Bái Đính'),
    (N'TN21                ', N'Bai Dinh'),
    (N'TN21                ', N'Hoa Lư'),
    (N'TN21                ', N'Hoa Lu'),
    (N'TN22                ', N'Nam Định'),
    (N'TN22                ', N'Nam Dinh'),
    (N'TN23                ', N'Thái Bình'),
    (N'TN23                ', N'Thai Binh'),
    (N'TN24                ', N'Hà Nam'),
    (N'TN24                ', N'Ha Nam'),
    (N'TN24                ', N'Phủ Lý'),
    (N'TN24                ', N'Phu Ly'),
    (N'TN25                ', N'Bắc Kạn'),
    (N'TN25                ', N'Bac Kan'),
    (N'TN25                ', N'Bac Can'),
    (N'TN26                ', N'Thanh Hóa'),
    (N'TN26                ', N'Thanh Hoa'),
    (N'TN26                ', N'Sầm Sơn'),
    (N'TN26                ', N'Sam Son'),
    (N'TN27                ', N'Nghệ An'),
    (N'TN27                ', N'Nghe An'),
    (N'TN27                ', N'Vinh'),
    (N'TN27                ', N'Cửa Lò'),
    (N'TN27                ', N'Cua Lo'),
    (N'TN28                ', N'Hà Tĩnh'),
    (N'TN28                ', N'Ha Tinh'),
    (N'TN29                ', N'Quảng Bình'),
    (N'TN29                ', N'Quang Binh'),
    (N'TN29                ', N'Đồng Hới'),
    (N'TN29                ', N'Dong Hoi'),
    (N'TN29                ', N'Phong Nha'),
    (N'TN30                ', N'Quảng Trị'),
    (N'TN30                ', N'Quang Tri'),
    (N'TN30                ', N'Đông Hà'),
    (N'TN30                ', N'Dong Ha'),
    (N'TN31                ', N'Thừa Thiên Huế'),
    (N'TN31                ', N'Thua Thien Hue'),
    (N'TN31                ', N'Huế'),
    (N'TN31                ', N'Hue'),
    (N'TN31                ', N'Đại Nội'),
    (N'TN31                ', N'Dai Noi'),
    (N'TN32                ', N'Đà Nẵng'),
    (N'TN32                ', N'Da Nang'),
    (N'TN32                ', N'Danang'),
    (N'TN32                ', N'Sơn Trà'),
    (N'TN32                ', N'Son Tra'),
    (N'TN32                ', N'Bà Nà'),
    (N'TN32                ', N'Ba Na'),
    (N'TN32                ', N'Mỹ Khê'),
    (N'TN32                ', N'My Khe'),
    (N'TN33                ', N'Quảng Nam'),
    (N'TN33                ', N'Quang Nam'),
    (N'TN33                ', N'Hội An'),
    (N'TN33                ', N'Hoi An'),
    (N'TN33                ', N'Mỹ Sơn'),
    (N'TN33                ', N'My Son'),
    (N'TN33                ', N'Tam Kỳ'),
    (N'TN33                ', N'Tam Ky'),
    (N'TN34                ', N'Quảng Ngãi'),
    (N'TN34                ', N'Quang Ngai'),
    (N'TN34                ', N'Lý Sơn'),
    (N'TN34                ', N'Ly Son'),
    (N'TN35                ', N'Bình Định'),
    (N'TN35                ', N'Binh Dinh'),
    (N'TN35                ', N'Quy Nhơn'),
    (N'TN35                ', N'Quy Nhon'),
    (N'TN36                ', N'Phú Yên'),
    (N'TN36                ', N'Phu Yen'),
    (N'TN36                ', N'Tuy Hòa'),
    (N'TN36                ', N'Tuy Hoa'),
    (N'TN37                ', N'Khánh Hòa'),
    (N'TN37                ', N'Khanh Hoa'),
    (N'TN37                ', N'Nha Trang'),
    (N'TN37                ', N'Nhatrang'),
    (N'TN37                ', N'VinWonders Nha Trang'),
    (N'TN37                ', N'Hòn Mun'),
    (N'TN37                ', N'Hon Mun'),
    (N'TN38                ', N'Ninh Thuận'),
    (N'TN38                ', N'Ninh Thuan'),
    (N'TN38                ', N'Phan Rang'),
    (N'TN39                ', N'Bình Thuận'),
    (N'TN39                ', N'Binh Thuan'),
    (N'TN39                ', N'Phan Thiết'),
    (N'TN39                ', N'Phan Thiet'),
    (N'TN39                ', N'Mũi Né'),
    (N'TN39                ', N'Mui Ne'),
    (N'TN40                ', N'Kon Tum'),
    (N'TN40                ', N'Kontum'),
    (N'TN41                ', N'Gia Lai'),
    (N'TN41                ', N'Pleiku'),
    (N'TN42                ', N'Đắk Lắk'),
    (N'TN42                ', N'Dak Lak'),
    (N'TN42                ', N'Dac Lac'),
    (N'TN42                ', N'Buôn Ma Thuột'),
    (N'TN42                ', N'Buon Ma Thuot'),
    (N'TN42                ', N'Ban Me Thuot'),
    (N'TN43                ', N'Đắk Nông'),
    (N'TN43                ', N'Dak Nong'),
    (N'TN43                ', N'Gia Nghĩa'),
    (N'TN43                ', N'Gia Nghia'),
    (N'TN44                ', N'Lâm Đồng'),
    (N'TN44                ', N'Lam Dong'),
    (N'TN44                ', N'Đà Lạt'),
    (N'TN44                ', N'Da Lat'),
    (N'TN44                ', N'Dalat'),
    (N'TN44                ', N'Hồ Xuân Hương'),
    (N'TN45                ', N'TP. Hồ Chí Minh'),
    (N'TN45                ', N'TP.HCM'),
    (N'TN45                ', N'TPHCM'),
    (N'TN45                ', N'Hồ Chí Minh'),
    (N'TN45                ', N'Ho Chi Minh'),
    (N'TN45                ', N'Sài Gòn'),
    (N'TN45                ', N'Sai Gon'),
    (N'TN45                ', N'Saigon'),
    (N'TN45                ', N'Bến Thành'),
    (N'TN45                ', N'Ben Thanh'),
    (N'TN46                ', N'Đồng Nai'),
    (N'TN46                ', N'Dong Nai'),
    (N'TN46                ', N'Biên Hòa'),
    (N'TN46                ', N'Bien Hoa'),
    (N'TN47                ', N'Bình Dương'),
    (N'TN47                ', N'Binh Duong'),
    (N'TN47                ', N'Thủ Dầu Một'),
    (N'TN47                ', N'Thu Dau Mot'),
    (N'TN48                ', N'Bà Rịa - Vũng Tàu'),
    (N'TN48                ', N'Ba Ria Vung Tau'),
    (N'TN48                ', N'Vũng Tàu'),
    (N'TN48                ', N'Vung Tau'),
    (N'TN48                ', N'Bà Rịa'),
    (N'TN48                ', N'Ba Ria'),
    (N'TN49                ', N'Tây Ninh'),
    (N'TN49                ', N'Tay Ninh'),
    (N'TN49                ', N'Núi Bà Đen'),
    (N'TN49                ', N'Nui Ba Den'),
    (N'TN50                ', N'Bình Phước'),
    (N'TN50                ', N'Binh Phuoc'),
    (N'TN50                ', N'Đồng Xoài'),
    (N'TN50                ', N'Dong Xoai'),
    (N'TN51                ', N'Long An'),
    (N'TN51                ', N'Tan An'),
    (N'TN51                ', N'Tân An'),
    (N'TN52                ', N'Tiền Giang'),
    (N'TN52                ', N'Tien Giang'),
    (N'TN52                ', N'Mỹ Tho'),
    (N'TN52                ', N'My Tho'),
    (N'TN53                ', N'Bến Tre'),
    (N'TN53                ', N'Ben Tre'),
    (N'TN54                ', N'Vĩnh Long'),
    (N'TN54                ', N'Vinh Long'),
    (N'TN55                ', N'Trà Vinh'),
    (N'TN55                ', N'Tra Vinh'),
    (N'TN56                ', N'Đồng Tháp'),
    (N'TN56                ', N'Dong Thap'),
    (N'TN56                ', N'Cao Lãnh'),
    (N'TN56                ', N'Cao Lanh'),
    (N'TN56                ', N'Sa Đéc'),
    (N'TN56                ', N'Sa Dec'),
    (N'TN57                ', N'An Giang'),
    (N'TN57                ', N'Long Xuyên'),
    (N'TN57                ', N'Long Xuyen'),
    (N'TN57                ', N'Châu Đốc'),
    (N'TN57                ', N'Chau Doc'),
    (N'TN58                ', N'Kiên Giang'),
    (N'TN58                ', N'Kien Giang'),
    (N'TN58                ', N'Phú Quốc'),
    (N'TN58                ', N'Phu Quoc'),
    (N'TN58                ', N'Rạch Giá'),
    (N'TN58                ', N'Rach Gia'),
    (N'TN58                ', N'Bãi Sao'),
    (N'TN58                ', N'Bai Sao'),
    (N'TN58                ', N'Hà Tiên'),
    (N'TN58                ', N'Ha Tien'),
    (N'TN59                ', N'Cần Thơ'),
    (N'TN59                ', N'Can Tho'),
    (N'TN59                ', N'Cái Răng'),
    (N'TN59                ', N'Cai Rang'),
    (N'TN59                ', N'Ninh Kiều'),
    (N'TN59                ', N'Ninh Kieu'),
    (N'TN60                ', N'Hậu Giang'),
    (N'TN60                ', N'Hau Giang'),
    (N'TN60                ', N'Vị Thanh'),
    (N'TN60                ', N'Vi Thanh'),
    (N'TN61                ', N'Sóc Trăng'),
    (N'TN61                ', N'Soc Trang'),
    (N'TN62                ', N'Bạc Liêu'),
    (N'TN62                ', N'Bac Lieu'),
    (N'TN63                ', N'Cà Mau'),
    (N'TN63                ', N'Ca Mau')
) AS s(MaTinh, TenAlias)
ON t.TenAlias = s.TenAlias
WHEN NOT MATCHED THEN INSERT (MaTinh, TenAlias) VALUES (s.MaTinh, s.TenAlias);
GO

-- Điểm/đối tác seed cũ (014/017) còn thiếu MaTinh
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN32                ' WHERE MaDThamQuan LIKE N'DT004%' AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN15                ' WHERE (TenDiaDanh LIKE N'%Fansipan%' OR TenDiaDanh LIKE N'%Sa Pa%' OR TenDiaDanh LIKE N'%Sapa%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN21                ' WHERE (TenDiaDanh LIKE N'%Tràng An%' OR TenDiaDanh LIKE N'%Trang An%' OR TenDiaDanh LIKE N'%Tam Cốc%' OR TenDiaDanh LIKE N'%Tam Coc%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN31                ' WHERE (TenDiaDanh LIKE N'%Đại Nội%' OR TenDiaDanh LIKE N'%Sông Hương%' OR TenDiaDanh LIKE N'%Huế%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN58                ' WHERE (TenDiaDanh LIKE N'%Bãi Sao%' OR TenDiaDanh LIKE N'%Phú Quốc%' OR TenDiaDanh LIKE N'%Hòn Thơm%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN59                ' WHERE (TenDiaDanh LIKE N'%Cần Thơ%' OR TenDiaDanh LIKE N'%Cái Răng%' OR TenDiaDanh LIKE N'%Ninh Kiều%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN44                ' WHERE (TenDiaDanh LIKE N'%Đà Lạt%' OR TenDiaDanh LIKE N'%Xuân Hương%' OR DiaChi LIKE N'%Đà Lạt%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN32                ' WHERE (TenDiaDanh LIKE N'%Chăm%' OR DiaChi LIKE N'%Đà Nẵng%') AND MaTinh IS NULL;
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN03                ' WHERE (TenDiaDanh LIKE N'%Hạ Long%' OR TenDiaDanh LIKE N'%Ha Long%') AND (MaTinh IS NULL OR MaTinh = N'TN01                ');
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN37                ' WHERE (TenDiaDanh LIKE N'%Nha Trang%' OR DiaChi LIKE N'%Nha Trang%') AND (MaTinh IS NULL OR MaTinh = N'TN01                ');
UPDATE dbo.DiemThamQuan SET MaTinh = N'TN33                ' WHERE (TenDiaDanh LIKE N'%Hội An%' OR DiaChi LIKE N'%Hội An%') AND MaTinh IS NULL;
UPDATE dbo.DoiTac SET MaTinh = N'TN33                ' WHERE TenDoiTac LIKE N'%Hội An%' AND MaTinh IS NULL;
UPDATE dbo.DoiTac SET MaTinh = N'TN45                ' WHERE (TenDoiTac LIKE N'%Bến Thành%' OR TenDoiTac LIKE N'%Sài Gòn%') AND MaTinh IS NULL;
UPDATE dbo.DoiTac SET MaTinh = N'TN37                ' WHERE TenDoiTac LIKE N'%Nha Trang%' AND MaTinh IS NULL;
UPDATE dbo.DoiTac SET MaTinh = N'TN01                ' WHERE MaDoiTac = N'DTAC001               ' AND MaTinh IS NULL;

-- Khớp còn lại theo tên tỉnh / alias
UPDATE d SET d.MaTinh = t.MaTinh
FROM dbo.DiemThamQuan d
JOIN dbo.TinhThanh t ON d.MaTinh IS NULL AND (
    d.DiaChi LIKE N'%' + RTRIM(t.TenTinh) + N'%' OR
    d.TenDiaDanh LIKE N'%' + RTRIM(t.TenTinh) + N'%');

UPDATE d SET d.MaTinh = a.MaTinh
FROM dbo.DiemThamQuan d
JOIN dbo.TinhThanhAlias a ON d.MaTinh IS NULL AND LEN(RTRIM(a.TenAlias)) >= 4 AND (
    d.DiaChi LIKE N'%' + RTRIM(a.TenAlias) + N'%' OR
    d.TenDiaDanh LIKE N'%' + RTRIM(a.TenAlias) + N'%');

UPDATE p SET p.MaTinh = t.MaTinh
FROM dbo.DoiTac p
JOIN dbo.TinhThanh t ON p.MaTinh IS NULL AND p.TenDoiTac LIKE N'%' + RTRIM(t.TenTinh) + N'%';

UPDATE p SET p.MaTinh = a.MaTinh
FROM dbo.DoiTac p
JOIN dbo.TinhThanhAlias a ON p.MaTinh IS NULL AND LEN(RTRIM(a.TenAlias)) >= 4 AND p.TenDoiTac LIKE N'%' + RTRIM(a.TenAlias) + N'%';

GO
