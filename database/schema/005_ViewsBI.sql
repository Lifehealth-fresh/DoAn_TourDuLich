/* Các view chỉ đọc dùng cho Power BI. Giá trị nchar được RTRIM để dữ liệu dễ dùng hơn. */

CREATE OR ALTER VIEW dbo.vw_DoanhThuTheoTour
AS
SELECT
    RTRIM(t.MaTour) AS MaTour,
    t.TenTour,
    SUM(ISNULL(tt.DaXacNhan, 0)) AS TongThanhTien,
    COUNT_BIG(*) AS SoLuongBooking
FROM dbo.DatDichVu d
INNER JOIN dbo.Tour t ON t.MaTour = d.MaTour
OUTER APPLY
(
    SELECT SUM(p.SoTien) AS DaXacNhan
    FROM dbo.ThanhToan p
    WHERE p.MaBooking = d.MaBooking
      AND RTRIM(p.TrangThai) IN (N'DaXacNhan', N'ThanhCong')
) tt
WHERE ISNULL(tt.DaXacNhan, 0) > 0
GROUP BY RTRIM(t.MaTour), t.TenTour;
GO

/* Reusable payment truth for BI; do not recompute outstanding balances in DAX. */
CREATE OR ALTER VIEW dbo.vw_ThanhToanTheoBooking
AS
SELECT
    RTRIM(d.MaBooking) AS MaBooking,
    RTRIM(d.MaTour) AS MaTour,
    d.ThanhTien,
    ISNULL(SUM(CASE WHEN RTRIM(p.TrangThai) IN (N'DaXacNhan', N'ThanhCong') THEN p.SoTien ELSE 0 END), 0) AS DaThanhToan,
    ISNULL(SUM(CASE WHEN RTRIM(p.TrangThai) = N'ChoXacNhan' THEN p.SoTien ELSE 0 END), 0) AS DangCho,
    d.ThanhTien - ISNULL(SUM(CASE WHEN RTRIM(p.TrangThai) IN (N'DaXacNhan', N'ThanhCong') THEN p.SoTien ELSE 0 END), 0) AS ConLai
FROM dbo.DatDichVu d
LEFT JOIN dbo.ThanhToan p ON p.MaBooking = d.MaBooking
GROUP BY RTRIM(d.MaBooking), RTRIM(d.MaTour), d.ThanhTien;
GO

CREATE OR ALTER VIEW dbo.vw_BookingTheoThang
AS
SELECT
    YEAR(d.NgayDat) AS Nam,
    MONTH(d.NgayDat) AS Thang,
    RTRIM(d.TrangThai) AS TrangThai,
    COUNT_BIG(*) AS SoLuongBooking
FROM dbo.DatDichVu d
WHERE d.NgayDat IS NOT NULL
GROUP BY YEAR(d.NgayDat), MONTH(d.NgayDat), RTRIM(d.TrangThai);
GO

CREATE OR ALTER VIEW dbo.vw_TyLeHuyBooking
AS
SELECT
    RTRIM(t.MaTour) AS MaTour,
    t.TenTour,
    SUM(CASE WHEN RTRIM(d.TrangThai) = N'DaHuy' THEN 1 ELSE 0 END) AS SoBookingDaHuy,
    COUNT_BIG(*) AS TongSoBooking,
    CAST(SUM(CASE WHEN RTRIM(d.TrangThai) = N'DaHuy' THEN 1.0 ELSE 0.0 END) / NULLIF(COUNT_BIG(*), 0) AS decimal(9,4)) AS TyLeHuy
FROM dbo.DatDichVu d
INNER JOIN dbo.Tour t ON t.MaTour = d.MaTour
GROUP BY RTRIM(t.MaTour), t.TenTour;
GO

CREATE OR ALTER VIEW dbo.vw_DanhGiaTrungBinhTour
AS
SELECT
    RTRIM(t.MaTour) AS MaTour,
    t.TenTour,
    AVG(CAST(dg.SaoDanhGia AS decimal(10,2))) AS DiemTrungBinh,
    COUNT_BIG(*) AS SoLuongDanhGia
FROM dbo.DanhGiaTour dg
INNER JOIN dbo.Tour t ON t.MaTour = dg.MaTour
GROUP BY RTRIM(t.MaTour), t.TenTour;
GO
