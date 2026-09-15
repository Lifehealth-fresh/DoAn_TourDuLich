/* 027: Đánh giá công khai vs nội bộ + KPI admin.
   Chạy SAU 026. SSMS UTF-8. Không xóa AnhTour / catalog.
   CongKhai=1: hiện website khách. =0: feedback tự thiết kế (nội bộ).
   Module QuyenNhanVien DanhGia: Them=1 chỉ để được XEM; admin không thêm/sửa/xóa bài. */
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.DanhGiaTour', N'CongKhai') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGiaTour ADD CongKhai bit NOT NULL
        CONSTRAINT DF_DanhGiaTour_CongKhai DEFAULT (1);
END
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DanhGiaTour_CongKhai_ThoiGian'
      AND object_id = OBJECT_ID(N'dbo.DanhGiaTour'))
    CREATE INDEX IX_DanhGiaTour_CongKhai_ThoiGian
        ON dbo.DanhGiaTour (CongKhai, ThoiGian DESC);
GO
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_QuyenNhanVien_ChucNang'
      AND parent_object_id = OBJECT_ID(N'dbo.QuyenNhanVien'))
    ALTER TABLE dbo.QuyenNhanVien DROP CONSTRAINT CK_QuyenNhanVien_ChucNang;
GO
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
INSERT INTO dbo.QuyenNhanVien (MaQuyen, MaUser, ChucNang, Them, Sua, Xoa, ToanQuyen)
SELECT CONVERT(nchar(20), N'QNDG' + RIGHT(RTRIM(u.MaUser), 4)),
       u.MaUser, N'DanhGia', 1, 0, 0, 0
FROM dbo.NguoiSuDung u
INNER JOIN dbo.VaiTro v ON v.MaVaiTro = u.MaVaiTro
WHERE RTRIM(v.TenVaiTro) IN (N'Admin', N'Sale')
  AND NOT EXISTS (
        SELECT 1 FROM dbo.QuyenNhanVien q
        WHERE q.MaUser = u.MaUser AND q.ChucNang = N'DanhGia');
GO

INSERT INTO dbo.DanhGiaTour (MaDanhGiaTour, MaUser, MaTour, ThoiGian, SaoDanhGia, NhanXet, CongKhai)
SELECT v.MaDanhGiaTour, v.MaUser, v.MaTour, v.ThoiGian, v.SaoDanhGia, v.NhanXet, v.CongKhai
FROM (VALUES
  (CAST(N'DG0001' AS nchar(20)), CAST(N'USR0011' AS nchar(20)), CAST(N'TOUR001' AS nchar(20)), DATEADD(DAY, -8, SYSUTCDATETIME()), 5, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0002' AS nchar(20)), CAST(N'USR0011' AS nchar(20)), CAST(N'TOUR002' AS nchar(20)), DATEADD(DAY, -15, SYSUTCDATETIME()), 5, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0003' AS nchar(20)), CAST(N'USR0011' AS nchar(20)), CAST(N'TOUR003' AS nchar(20)), DATEADD(DAY, -22, SYSUTCDATETIME()), 5, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0004' AS nchar(20)), CAST(N'USR0012' AS nchar(20)), CAST(N'TOUR004' AS nchar(20)), DATEADD(DAY, -29, SYSUTCDATETIME()), 5, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0005' AS nchar(20)), CAST(N'USR0012' AS nchar(20)), CAST(N'TOUR005' AS nchar(20)), DATEADD(DAY, -36, SYSUTCDATETIME()), 5, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0006' AS nchar(20)), CAST(N'USR0012' AS nchar(20)), CAST(N'TOUR006' AS nchar(20)), DATEADD(DAY, -43, SYSUTCDATETIME()), 4, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0007' AS nchar(20)), CAST(N'USR0013' AS nchar(20)), CAST(N'TOUR007' AS nchar(20)), DATEADD(DAY, -50, SYSUTCDATETIME()), 4, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0008' AS nchar(20)), CAST(N'USR0013' AS nchar(20)), CAST(N'TOUR008' AS nchar(20)), DATEADD(DAY, -57, SYSUTCDATETIME()), 4, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0009' AS nchar(20)), CAST(N'USR0013' AS nchar(20)), CAST(N'TOUR009' AS nchar(20)), DATEADD(DAY, -64, SYSUTCDATETIME()), 3, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0010' AS nchar(20)), CAST(N'USR0014' AS nchar(20)), CAST(N'TOUR010' AS nchar(20)), DATEADD(DAY, -71, SYSUTCDATETIME()), 5, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0011' AS nchar(20)), CAST(N'USR0014' AS nchar(20)), CAST(N'TOUR011' AS nchar(20)), DATEADD(DAY, -78, SYSUTCDATETIME()), 2, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0012' AS nchar(20)), CAST(N'USR0014' AS nchar(20)), CAST(N'TOUR012' AS nchar(20)), DATEADD(DAY, -85, SYSUTCDATETIME()), 5, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0013' AS nchar(20)), CAST(N'USR0015' AS nchar(20)), CAST(N'TOUR013' AS nchar(20)), DATEADD(DAY, -92, SYSUTCDATETIME()), 4, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0014' AS nchar(20)), CAST(N'USR0015' AS nchar(20)), CAST(N'TOUR014' AS nchar(20)), DATEADD(DAY, -99, SYSUTCDATETIME()), 1, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0015' AS nchar(20)), CAST(N'USR0015' AS nchar(20)), CAST(N'TOUR015' AS nchar(20)), DATEADD(DAY, -106, SYSUTCDATETIME()), 5, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0016' AS nchar(20)), CAST(N'USR0016' AS nchar(20)), CAST(N'TOUR016' AS nchar(20)), DATEADD(DAY, -113, SYSUTCDATETIME()), 5, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0017' AS nchar(20)), CAST(N'USR0016' AS nchar(20)), CAST(N'TOUR017' AS nchar(20)), DATEADD(DAY, -120, SYSUTCDATETIME()), 4, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0018' AS nchar(20)), CAST(N'USR0016' AS nchar(20)), CAST(N'TOUR018' AS nchar(20)), DATEADD(DAY, -127, SYSUTCDATETIME()), 5, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0019' AS nchar(20)), CAST(N'USR0017' AS nchar(20)), CAST(N'TOUR019' AS nchar(20)), DATEADD(DAY, -134, SYSUTCDATETIME()), 3, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0020' AS nchar(20)), CAST(N'USR0017' AS nchar(20)), CAST(N'TOUR020' AS nchar(20)), DATEADD(DAY, -141, SYSUTCDATETIME()), 4, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0021' AS nchar(20)), CAST(N'USR0017' AS nchar(20)), CAST(N'TOUR021' AS nchar(20)), DATEADD(DAY, -148, SYSUTCDATETIME()), 5, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0022' AS nchar(20)), CAST(N'USR0018' AS nchar(20)), CAST(N'TOUR022' AS nchar(20)), DATEADD(DAY, -155, SYSUTCDATETIME()), 5, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0023' AS nchar(20)), CAST(N'USR0018' AS nchar(20)), CAST(N'TOUR023' AS nchar(20)), DATEADD(DAY, -162, SYSUTCDATETIME()), 5, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0024' AS nchar(20)), CAST(N'USR0018' AS nchar(20)), CAST(N'TOUR024' AS nchar(20)), DATEADD(DAY, -169, SYSUTCDATETIME()), 5, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0025' AS nchar(20)), CAST(N'USR0019' AS nchar(20)), CAST(N'TOUR025' AS nchar(20)), DATEADD(DAY, -6, SYSUTCDATETIME()), 5, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0026' AS nchar(20)), CAST(N'USR0019' AS nchar(20)), CAST(N'TOUR026' AS nchar(20)), DATEADD(DAY, -13, SYSUTCDATETIME()), 4, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0027' AS nchar(20)), CAST(N'USR0019' AS nchar(20)), CAST(N'TOUR027' AS nchar(20)), DATEADD(DAY, -20, SYSUTCDATETIME()), 4, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0028' AS nchar(20)), CAST(N'USR0020' AS nchar(20)), CAST(N'TOUR028' AS nchar(20)), DATEADD(DAY, -27, SYSUTCDATETIME()), 4, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0029' AS nchar(20)), CAST(N'USR0020' AS nchar(20)), CAST(N'TOUR029' AS nchar(20)), DATEADD(DAY, -34, SYSUTCDATETIME()), 3, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0030' AS nchar(20)), CAST(N'USR0020' AS nchar(20)), CAST(N'TOUR030' AS nchar(20)), DATEADD(DAY, -41, SYSUTCDATETIME()), 5, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0031' AS nchar(20)), CAST(N'USR0021' AS nchar(20)), CAST(N'TOUR001' AS nchar(20)), DATEADD(DAY, -48, SYSUTCDATETIME()), 2, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0032' AS nchar(20)), CAST(N'USR0021' AS nchar(20)), CAST(N'TOUR002' AS nchar(20)), DATEADD(DAY, -55, SYSUTCDATETIME()), 5, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0033' AS nchar(20)), CAST(N'USR0021' AS nchar(20)), CAST(N'TOUR003' AS nchar(20)), DATEADD(DAY, -62, SYSUTCDATETIME()), 4, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0034' AS nchar(20)), CAST(N'USR0022' AS nchar(20)), CAST(N'TOUR004' AS nchar(20)), DATEADD(DAY, -69, SYSUTCDATETIME()), 1, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0035' AS nchar(20)), CAST(N'USR0022' AS nchar(20)), CAST(N'TOUR005' AS nchar(20)), DATEADD(DAY, -76, SYSUTCDATETIME()), 5, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0036' AS nchar(20)), CAST(N'USR0022' AS nchar(20)), CAST(N'TOUR006' AS nchar(20)), DATEADD(DAY, -83, SYSUTCDATETIME()), 5, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0037' AS nchar(20)), CAST(N'USR0023' AS nchar(20)), CAST(N'TOUR007' AS nchar(20)), DATEADD(DAY, -90, SYSUTCDATETIME()), 4, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0038' AS nchar(20)), CAST(N'USR0023' AS nchar(20)), CAST(N'TOUR008' AS nchar(20)), DATEADD(DAY, -97, SYSUTCDATETIME()), 5, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0039' AS nchar(20)), CAST(N'USR0023' AS nchar(20)), CAST(N'TOUR009' AS nchar(20)), DATEADD(DAY, -104, SYSUTCDATETIME()), 3, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0040' AS nchar(20)), CAST(N'USR0024' AS nchar(20)), CAST(N'TOUR010' AS nchar(20)), DATEADD(DAY, -111, SYSUTCDATETIME()), 4, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0041' AS nchar(20)), CAST(N'USR0024' AS nchar(20)), CAST(N'TOUR011' AS nchar(20)), DATEADD(DAY, -118, SYSUTCDATETIME()), 5, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0042' AS nchar(20)), CAST(N'USR0024' AS nchar(20)), CAST(N'TOUR012' AS nchar(20)), DATEADD(DAY, -125, SYSUTCDATETIME()), 5, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0043' AS nchar(20)), CAST(N'USR0025' AS nchar(20)), CAST(N'TOUR013' AS nchar(20)), DATEADD(DAY, -132, SYSUTCDATETIME()), 5, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0044' AS nchar(20)), CAST(N'USR0025' AS nchar(20)), CAST(N'TOUR014' AS nchar(20)), DATEADD(DAY, -139, SYSUTCDATETIME()), 5, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0045' AS nchar(20)), CAST(N'USR0025' AS nchar(20)), CAST(N'TOUR015' AS nchar(20)), DATEADD(DAY, -146, SYSUTCDATETIME()), 5, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0046' AS nchar(20)), CAST(N'USR0026' AS nchar(20)), CAST(N'TOUR016' AS nchar(20)), DATEADD(DAY, -153, SYSUTCDATETIME()), 4, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0047' AS nchar(20)), CAST(N'USR0026' AS nchar(20)), CAST(N'TOUR017' AS nchar(20)), DATEADD(DAY, -160, SYSUTCDATETIME()), 4, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0048' AS nchar(20)), CAST(N'USR0026' AS nchar(20)), CAST(N'TOUR018' AS nchar(20)), DATEADD(DAY, -167, SYSUTCDATETIME()), 4, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0049' AS nchar(20)), CAST(N'USR0027' AS nchar(20)), CAST(N'TOUR019' AS nchar(20)), DATEADD(DAY, -4, SYSUTCDATETIME()), 3, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0050' AS nchar(20)), CAST(N'USR0027' AS nchar(20)), CAST(N'TOUR020' AS nchar(20)), DATEADD(DAY, -11, SYSUTCDATETIME()), 5, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0051' AS nchar(20)), CAST(N'USR0027' AS nchar(20)), CAST(N'TOUR021' AS nchar(20)), DATEADD(DAY, -18, SYSUTCDATETIME()), 2, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0052' AS nchar(20)), CAST(N'USR0028' AS nchar(20)), CAST(N'TOUR022' AS nchar(20)), DATEADD(DAY, -25, SYSUTCDATETIME()), 5, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0053' AS nchar(20)), CAST(N'USR0028' AS nchar(20)), CAST(N'TOUR023' AS nchar(20)), DATEADD(DAY, -32, SYSUTCDATETIME()), 4, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0054' AS nchar(20)), CAST(N'USR0028' AS nchar(20)), CAST(N'TOUR024' AS nchar(20)), DATEADD(DAY, -39, SYSUTCDATETIME()), 1, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0055' AS nchar(20)), CAST(N'USR0029' AS nchar(20)), CAST(N'TOUR025' AS nchar(20)), DATEADD(DAY, -46, SYSUTCDATETIME()), 5, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0056' AS nchar(20)), CAST(N'USR0029' AS nchar(20)), CAST(N'TOUR026' AS nchar(20)), DATEADD(DAY, -53, SYSUTCDATETIME()), 5, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0057' AS nchar(20)), CAST(N'USR0029' AS nchar(20)), CAST(N'TOUR027' AS nchar(20)), DATEADD(DAY, -60, SYSUTCDATETIME()), 4, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0058' AS nchar(20)), CAST(N'USR0030' AS nchar(20)), CAST(N'TOUR028' AS nchar(20)), DATEADD(DAY, -67, SYSUTCDATETIME()), 5, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0059' AS nchar(20)), CAST(N'USR0030' AS nchar(20)), CAST(N'TOUR029' AS nchar(20)), DATEADD(DAY, -74, SYSUTCDATETIME()), 3, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0060' AS nchar(20)), CAST(N'USR0030' AS nchar(20)), CAST(N'TOUR030' AS nchar(20)), DATEADD(DAY, -81, SYSUTCDATETIME()), 4, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0061' AS nchar(20)), CAST(N'USR0031' AS nchar(20)), CAST(N'TOUR001' AS nchar(20)), DATEADD(DAY, -88, SYSUTCDATETIME()), 5, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0062' AS nchar(20)), CAST(N'USR0031' AS nchar(20)), CAST(N'TOUR002' AS nchar(20)), DATEADD(DAY, -95, SYSUTCDATETIME()), 5, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0063' AS nchar(20)), CAST(N'USR0031' AS nchar(20)), CAST(N'TOUR003' AS nchar(20)), DATEADD(DAY, -102, SYSUTCDATETIME()), 5, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0064' AS nchar(20)), CAST(N'USR0032' AS nchar(20)), CAST(N'TOUR004' AS nchar(20)), DATEADD(DAY, -109, SYSUTCDATETIME()), 5, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0065' AS nchar(20)), CAST(N'USR0032' AS nchar(20)), CAST(N'TOUR005' AS nchar(20)), DATEADD(DAY, -116, SYSUTCDATETIME()), 5, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0066' AS nchar(20)), CAST(N'USR0032' AS nchar(20)), CAST(N'TOUR006' AS nchar(20)), DATEADD(DAY, -123, SYSUTCDATETIME()), 4, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0067' AS nchar(20)), CAST(N'USR0033' AS nchar(20)), CAST(N'TOUR007' AS nchar(20)), DATEADD(DAY, -130, SYSUTCDATETIME()), 4, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0068' AS nchar(20)), CAST(N'USR0033' AS nchar(20)), CAST(N'TOUR008' AS nchar(20)), DATEADD(DAY, -137, SYSUTCDATETIME()), 4, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0069' AS nchar(20)), CAST(N'USR0033' AS nchar(20)), CAST(N'TOUR009' AS nchar(20)), DATEADD(DAY, -144, SYSUTCDATETIME()), 3, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0070' AS nchar(20)), CAST(N'USR0034' AS nchar(20)), CAST(N'TOUR010' AS nchar(20)), DATEADD(DAY, -151, SYSUTCDATETIME()), 5, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0071' AS nchar(20)), CAST(N'USR0034' AS nchar(20)), CAST(N'TOUR011' AS nchar(20)), DATEADD(DAY, -158, SYSUTCDATETIME()), 2, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0072' AS nchar(20)), CAST(N'USR0034' AS nchar(20)), CAST(N'TOUR012' AS nchar(20)), DATEADD(DAY, -165, SYSUTCDATETIME()), 5, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0073' AS nchar(20)), CAST(N'USR0035' AS nchar(20)), CAST(N'TOUR013' AS nchar(20)), DATEADD(DAY, -2, SYSUTCDATETIME()), 4, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0074' AS nchar(20)), CAST(N'USR0035' AS nchar(20)), CAST(N'TOUR014' AS nchar(20)), DATEADD(DAY, -9, SYSUTCDATETIME()), 1, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0075' AS nchar(20)), CAST(N'USR0035' AS nchar(20)), CAST(N'TOUR015' AS nchar(20)), DATEADD(DAY, -16, SYSUTCDATETIME()), 5, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0076' AS nchar(20)), CAST(N'USR0036' AS nchar(20)), CAST(N'TOUR016' AS nchar(20)), DATEADD(DAY, -23, SYSUTCDATETIME()), 5, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0077' AS nchar(20)), CAST(N'USR0036' AS nchar(20)), CAST(N'TOUR017' AS nchar(20)), DATEADD(DAY, -30, SYSUTCDATETIME()), 4, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0078' AS nchar(20)), CAST(N'USR0036' AS nchar(20)), CAST(N'TOUR018' AS nchar(20)), DATEADD(DAY, -37, SYSUTCDATETIME()), 5, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit)),
  (CAST(N'DG0079' AS nchar(20)), CAST(N'USR0037' AS nchar(20)), CAST(N'TOUR019' AS nchar(20)), DATEADD(DAY, -44, SYSUTCDATETIME()), 3, N'Lịch trình hơi dày, nên bớt một điểm buổi chiều.', CAST(1 AS bit)),
  (CAST(N'DG0080' AS nchar(20)), CAST(N'USR0037' AS nchar(20)), CAST(N'TOUR020' AS nchar(20)), DATEADD(DAY, -51, SYSUTCDATETIME()), 4, N'Gia đình rất hài lòng, trẻ em thích tắm biển.', CAST(1 AS bit)),
  (CAST(N'DG0081' AS nchar(20)), CAST(N'USR0037' AS nchar(20)), CAST(N'TOUR021' AS nchar(20)), DATEADD(DAY, -58, SYSUTCDATETIME()), 5, N'Thủ tục nhanh, hỗ trợ đổi ngày linh hoạt.', CAST(1 AS bit)),
  (CAST(N'DG0082' AS nchar(20)), CAST(N'USR0038' AS nchar(20)), CAST(N'TOUR022' AS nchar(20)), DATEADD(DAY, -65, SYSUTCDATETIME()), 5, N'Chưa tương xứng giá, cần cải thiện bữa trưa.', CAST(1 AS bit)),
  (CAST(N'DG0083' AS nchar(20)), CAST(N'USR0038' AS nchar(20)), CAST(N'TOUR023' AS nchar(20)), DATEADD(DAY, -72, SYSUTCDATETIME()), 5, N'Cảnh đẹp, thời tiết thuận, chụp ảnh rất đã.', CAST(1 AS bit)),
  (CAST(N'DG0084' AS nchar(20)), CAST(N'USR0038' AS nchar(20)), CAST(N'TOUR024' AS nchar(20)), DATEADD(DAY, -79, SYSUTCDATETIME()), 5, N'HDV quan tâm khách lớn tuổi, khen ngợi.', CAST(1 AS bit)),
  (CAST(N'DG0085' AS nchar(20)), CAST(N'USR0039' AS nchar(20)), CAST(N'TOUR025' AS nchar(20)), DATEADD(DAY, -86, SYSUTCDATETIME()), 5, N'Hướng dẫn nhiệt tình, lịch trình vừa sức.', CAST(1 AS bit)),
  (CAST(N'DG0086' AS nchar(20)), CAST(N'USR0039' AS nchar(20)), CAST(N'TOUR026' AS nchar(20)), DATEADD(DAY, -93, SYSUTCDATETIME()), 4, N'Khách sạn sạch, gần trung tâm, sẽ quay lại.', CAST(1 AS bit)),
  (CAST(N'DG0087' AS nchar(20)), CAST(N'USR0039' AS nchar(20)), CAST(N'TOUR027' AS nchar(20)), DATEADD(DAY, -100, SYSUTCDATETIME()), 4, N'Điểm tham quan đẹp nhưng đông khách vào cuối tuần.', CAST(1 AS bit)),
  (CAST(N'DG0088' AS nchar(20)), CAST(N'USR0040' AS nchar(20)), CAST(N'TOUR028' AS nchar(20)), DATEADD(DAY, -107, SYSUTCDATETIME()), 4, N'Bữa ăn ngon, đoàn vui, giá hợp lý.', CAST(1 AS bit)),
  (CAST(N'DG0089' AS nchar(20)), CAST(N'USR0040' AS nchar(20)), CAST(N'TOUR029' AS nchar(20)), DATEADD(DAY, -114, SYSUTCDATETIME()), 3, N'Xe đưa đón đúng giờ, HDV giải thích dễ hiểu.', CAST(1 AS bit)),
  (CAST(N'DG0090' AS nchar(20)), CAST(N'USR0040' AS nchar(20)), CAST(N'TOUR030' AS nchar(20)), DATEADD(DAY, -121, SYSUTCDATETIME()), 5, N'Phòng hơi nhỏ nhưng view đẹp, đáng tiền.', CAST(1 AS bit))
) v(MaDanhGiaTour, MaUser, MaTour, ThoiGian, SaoDanhGia, NhanXet, CongKhai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.DanhGiaTour d WHERE d.MaDanhGiaTour = v.MaDanhGiaTour)
  AND EXISTS (SELECT 1 FROM dbo.NguoiSuDung u WHERE u.MaUser = v.MaUser)
  AND EXISTS (SELECT 1 FROM dbo.Tour t WHERE t.MaTour = v.MaTour);
GO

UPDATE d SET CongKhai = CASE WHEN RTRIM(t.LoaiTour) = N'TuThietKe' THEN 0 ELSE 1 END
FROM dbo.DanhGiaTour d
INNER JOIN dbo.Tour t ON t.MaTour = d.MaTour
WHERE d.CongKhai IS NULL OR (RTRIM(t.LoaiTour) = N'TuThietKe' AND d.CongKhai = 1);
GO
SELECT N'DanhGiaTour' AS Bang, COUNT(*) AS SoDong,
       SUM(CASE WHEN CongKhai = 1 THEN 1 ELSE 0 END) AS CongKhai,
       SUM(CASE WHEN CongKhai = 0 THEN 1 ELSE 0 END) AS NoiBo,
       CAST(AVG(CAST(SaoDanhGia AS float)) AS decimal(4,2)) AS DiemTb
FROM dbo.DanhGiaTour;
