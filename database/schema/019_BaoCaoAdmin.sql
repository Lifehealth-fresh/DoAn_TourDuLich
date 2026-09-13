/* View đọc cho trang tổng quan admin và Power BI.
   Mẫu số chỗ trống khớp DepartureAvailability (loại DaHuy / ChoHoanTien).
   Doanh thu chỉ cộng thanh toán DaXacNhan / ThanhCong. */

CREATE OR ALTER VIEW dbo.vw_DoanhThuTheoThang
AS
SELECT
    YEAR(ISNULL(p.PaidAt, p.NgayTt)) AS Nam,
    MONTH(ISNULL(p.PaidAt, p.NgayTt)) AS Thang,
    SUM(ISNULL(p.SoTien, 0)) AS DaThu,
    COUNT_BIG(*) AS SoGiaoDich
FROM dbo.ThanhToan p
WHERE RTRIM(p.TrangThai) IN (N'DaXacNhan', N'ThanhCong')
  AND ISNULL(p.PaidAt, p.NgayTt) IS NOT NULL
GROUP BY YEAR(ISNULL(p.PaidAt, p.NgayTt)), MONTH(ISNULL(p.PaidAt, p.NgayTt));
GO

CREATE OR ALTER VIEW dbo.vw_BookingTheoTrangThai
AS
SELECT
    RTRIM(d.TrangThai) AS TrangThai,
    COUNT_BIG(*) AS SoLuong,
    SUM(ISNULL(d.ThanhTien, 0)) AS ThanhTien
FROM dbo.DatDichVu d
GROUP BY RTRIM(d.TrangThai);
GO

CREATE OR ALTER VIEW dbo.vw_ChoTrongTheoLich
AS
SELECT
    RTRIM(l.MaKhoiHanh) AS MaKhoiHanh,
    RTRIM(l.MaTour) AS MaTour,
    t.TenTour,
    l.NgayKhoiHanh,
    l.NgayKetThuc,
    ISNULL(l.SoCho, t.Slkhach) AS SucChua,
    ISNULL(held.DaDat, 0) AS DaDat,
    ISNULL(l.SoCho, t.Slkhach) - ISNULL(held.DaDat, 0) AS ConTrong,
    CAST(
        CASE WHEN ISNULL(l.SoCho, t.Slkhach) = 0 THEN 0
             ELSE 1.0 * ISNULL(held.DaDat, 0) / ISNULL(l.SoCho, t.Slkhach)
        END AS decimal(9, 4)
    ) AS TyLeLapDay
FROM dbo.LichKhoiHanh l
INNER JOIN dbo.Tour t ON t.MaTour = l.MaTour
OUTER APPLY
(
    SELECT SUM(ISNULL(d.SlnguoiLon, 0) + ISNULL(d.SltreEm, 0)) AS DaDat
    FROM dbo.DatDichVu d
    WHERE d.MaKhoiHanh = l.MaKhoiHanh
      AND RTRIM(d.TrangThai) NOT IN (N'DaHuy', N'ChoHoanTien')
) held;
GO
