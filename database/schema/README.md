# Database scripts

Bộ script này dựng schema và dữ liệu test cho Azure SQL trong bối cảnh đã chọn đúng
database. Không có `CREATE DATABASE` hoặc `USE`; hãy mở đúng database trước khi chạy.

## Thứ tự chạy

1. `001_CreateSchema.sql` — tạo 31 bảng, khóa, FK, constraint và cột tính toán.
2. `002_SeedVaiTro.sql` — tạo `Admin`, `Sale`, `KhachHang` nếu chưa có.
3. `003_SeedTestData.sql` — thêm dữ liệu mẫu và 4 tài khoản test.
4. `004_Indexes.sql` — thêm index truy vấn; script có kiểm tra tránh tạo trùng.
5. `005_ViewsBI.sql` — tạo/cập nhật các view đọc cho Power BI.
6. `006_HopDong_TuDongDien.sql` — migration bổ sung hồ sơ đại diện booking và snapshot hợp đồng sau khi đã có schema 001-005.
7. `007_LichTrinhDeXuat.sql` — bổ sung staging cho các phương án lịch trình tự thiết kế.
8. `008_AnhTour_Video.sql` — bổ sung loại media và cột `Url` tương thích ngược cho `AnhTour`.
9. `009_MediaDanhGia.sql` — tạo ba bảng media có FK cứng cho các loại đánh giá.
10. `010_ThanhToan_Idempotency.sql` — bổ sung `Idempotency-Key` cho retry thanh toán an toàn.
11. `011_JobRunLog.sql` — nhật ký chạy job làm mới gợi ý AI.
12. `012_AnhTour_Cloudinary.sql` — lưu Cloudinary public ID để thay thế/xóa media an toàn.

Thứ tự chạy thực tế là `001 → 002 → 003 → 004 → 005 → 006 → 007 → 008 → 009 → 010 → 011 → 012`.

Tất cả tài khoản test trong bước 3 dùng mật khẩu `Test@123456`. Mật khẩu được lưu
bằng BCrypt hash cố định được tạo bằng package `BCrypt.Net-Next` phiên bản `4.2.0`
đang dùng trong solution.

## Cảnh báo

`001_CreateSchema.sql` dành cho database trống hoàn toàn. Không chạy file này trên
database Azure SQL đang có dữ liệu thật vì sẽ lỗi khi tạo bảng/khóa đã tồn tại.
Các file seed, index và view có thể chạy lại theo điều kiện idempotent của từng file,
nhưng vẫn nên sao lưu database trước khi thao tác.

Schema được đối chiếu với entity và Fluent API hiện tại trong
`backend/src/TourDuLich.Infrastructure/Entities/` và `AppDbContext.cs`.

`006_HopDong_TuDongDien.sql` là migration cho database đã dựng từ 001-005;
không chạy lại nếu các cột/constraint đã tồn tại.
