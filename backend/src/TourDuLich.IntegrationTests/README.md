# TourDuLich integration tests

Đây là bộ test gọi HTTP vào API thật bằng `WebApplicationFactory<Program>` và dùng
Azure SQL thật, không mock `AppDbContext`.

## Cấu hình

Ưu tiên đặt connection string database test/dev riêng:

```powershell
$env:TOURDULICH_TEST_CONNECTION = "Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;"
```

Nếu không đặt biến này, test đọc `TourDuLich.API/appsettings.Development.json`.
Tài khoản Sale/Admin mặc định theo seed database là:

```powershell
$env:TOURDULICH_TEST_ADMIN_PHONE = "0900000001"
$env:TOURDULICH_TEST_ADMIN_PASSWORD = "Test@123456"
$env:TOURDULICH_TEST_SALE_PHONE = "0900000002"
$env:TOURDULICH_TEST_SALE_PASSWORD = "Test@123456"
```

Mỗi tài khoản khách test dùng số điện thoại bắt đầu bằng `0777`. Test tự dọn các
dòng dữ liệu tạo bởi nhóm này trong `DisposeAsync`. Không chạy nhiều phiên
`dotnet test` song song trên cùng Azure SQL vì các test dùng chung dữ liệu seed.

## Chạy

```powershell
dotnet test backend/TourDuLich.slnx
```

Các test cần database đã dựng bằng `database/schema/001_CreateSchema.sql` đến
`003_SeedTestData.sql`, sau đó chạy migration `database/schema/006_HopDong_TuDongDien.sql`.
Test khuyến mãi cần seed mã `SUMMER001`; nếu thiếu mã,
test đó được đánh dấu skip thay vì sửa logic nghiệp vụ.

## Nhóm test

- `AuthTests`: 1 test, đăng ký, đăng nhập sai/đúng và kiểm tra JWT claim.
- `RbacTests`: 3 test, quyền ghi tour, quyền Admin và gửi duyệt.
- `OwnershipTests`: 3 test, hồ sơ, booking và danh sách yêu thích.
- `BookingTests`: 3 test, lịch quá hạn, trạng thái, sức chứa và chuyển trạng thái.
- `PaymentTests`: 2 test, booking hủy, giới hạn tiền và quyền xem thanh toán.
- `PromotionTests`: 1 test, áp mã, tính giảm giá và chống áp trùng.
- `SelfDesignedTourPrivacyTests`: 1 test, quyền riêng tư tour tự thiết kế.
- `HopDongTests`: 3 test, snapshot hồ sơ, quyền Sale/Admin và chặn hồ sơ người khác.
- `CancellationTests`: 8 test case, tỷ lệ phạt, giới hạn tiền phạt và trạng thái hủy.

Tổng cộng: **25 test case**.
