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

21. `019_BaoCaoAdmin.sql` — view doanh thu theo tháng, booking theo trạng thái, chỗ trống theo lịch khởi hành (trang tổng quan admin + Power BI).
22. `020_QuyenTaiKhoan.sql` — bảng `QuyenNhanVien`: phân quyền Thêm/Sửa/Xóa/Toàn quyền theo từng tài khoản và từng chức năng admin.
23. `021_RefreshToken.sql` — bảng phiên refresh token (thu hồi khi đăng xuất, xoay token khi gia hạn).
24. `022_SeedCatalogMoRong.sql` — thêm tour đang bán TOUR010–TOUR018, lịch khởi hành có sức chứa, mã ưu đãi, đánh giá mẫu. Ảnh tour để trống, upload sau trên admin.
25. `023_TuThietKeChatPlanner.sql` — bảng chat tự thiết kế, alias tỉnh (Nha Trang→Khánh Hòa), ma trận thời gian đi, backfill MaTinh điểm cũ. **Không xóa booking/user.** Chạy sau 018+022.
26. `024_MatchToanBoOffline.sql` — alias 63 tỉnh + thành phố thường gõ; backfill MaTinh còn thiếu (014/017). Không cần Google Maps. Chạy sau 023.

Thứ tự chạy thực tế là `001 → 002 → 003 → 004 → 005 → 006 → 007 → 008 → 009 → 010 → 011 → 012`.
Các file 013–022 là migration bổ sung, chạy theo số thứ tự trên database đã có schema.

Tất cả tài khoản test trong bước 3 dùng mật khẩu `Test@123456` **chỉ trên database local/dev**.
Trên Azure production phải đổi mật khẩu seed ngay sau khi dựng, không để tài khoản demo công khai.

## Cảnh báo

`001_CreateSchema.sql` dành cho database trống hoàn toàn. Không chạy file này trên
database Azure SQL đang có dữ liệu thật vì sẽ lỗi khi tạo bảng/khóa đã tồn tại.
Các file seed, index và view có thể chạy lại theo điều kiện idempotent của từng file,
nhưng vẫn nên sao lưu database trước khi thao tác.

Schema được đối chiếu với entity và Fluent API hiện tại trong
`backend/src/TourDuLich.Infrastructure/Entities/` và `AppDbContext.cs`.

`006_HopDong_TuDongDien.sql` là migration cho database đã dựng từ 001-005;
không chạy lại nếu các cột/constraint đã tồn tại.
