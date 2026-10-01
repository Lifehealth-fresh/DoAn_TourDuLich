<div align="center">

# Database TourDuLich

Ba file. Chỉ nhóm nào có trong script gốc mới được tách ra.

</div>

## Thứ tự chạy

Chỉ dùng khi database `TourDuLich` **trống**. Chọn đúng database trên thanh SSMS, encoding UTF-8, không có `USE`.

| Bước | File | Nội dung |
|---|---|---|
| 1 | [schema/001_TaoBang.sql](schema/001_TaoBang.sql) | `CREATE TABLE`, `ALTER TABLE`, khóa, chỉ mục |
| 2 | [schema/002_TaoView.sql](schema/002_TaoView.sql) | 8 view báo cáo (`CREATE OR ALTER VIEW`) |
| 3 | [schema/003_SeedDuLieuMau.sql](schema/003_SeedDuLieuMau.sql) | `INSERT` / `UPDATE` / `DELETE` của bảng còn dùng |

Database đang chạy đồ án: không chạy lại. File seed có transaction của script cũ `025` (xóa dữ liệu trung gian, giữ ảnh tour, rồi seed đầy đủ phía sau). Chạy nhầm lên Azure đang dùng sẽ mất dữ liệu.


## Bảng không đưa vào script

Các bảng sau không còn trong sản phẩm, nên không có `CREATE`, `INSERT`, `DELETE` hay `DROP`:

`HuongDanVien`, `LichDanTour`, `DanhGiaHDV`, `DanhGiaSanPhamDoiTac`, `ThongBao`, `Quyen` (bảng quyền cũ), `MediaDanhGiaHdv`, `MediaDanhGiaSanPham`, `JobRunLog`, `HoiThoaiThietKe`, `TinNhanThietKe`.

Phân quyền dùng `QuyenNhanVien`. Chat hỗ trợ dùng `CuocTroChuyen` / `TinNhanHoTro`. Ảnh đánh giá tour dùng `MediaDanhGiaTour`. Cột `Tour.SLHuongDanVien` vẫn còn trên bảng `Tour` vì code và dữ liệu tour vẫn có cột này; chỉ bỏ bảng hướng dẫn viên.

## Transaction

SQL Server không có lệnh "tạo transaction" như tạo bảng. Trong bộ script chỉ có **một** `BEGIN TRANSACTION`, nằm giữa file seed (khối cũ `025`): xóa dữ liệu tạm trước khi nạp bộ mẫu cuối. Tách khối đó ra file riêng sẽ sai thứ tự, nên giữ nguyên trong `003_SeedDuLieuMau.sql`.

## View

`vw_DoanhThuTheoTour`, `vw_ThanhToanTheoBooking`, `vw_BookingTheoThang` và các view trang tổng quan admin (`vw_DoanhThuTheoThang`, `vw_BookingTheoTrangThai`, `vw_ChoTrongTheoLich`, …). Không cần thư mục Power BI.

## Tài khoản sau khi seed

| Số điện thoại | Vai trò | Mật khẩu |
|---|---|---|
| `0900000001` | Admin | `Test@123456` |
| `0900000002` | Sale | `Test@123456` |
| `0900000003` | Khách | `Test@123456` |
| `0900000004` | Khách | `Test@123456` |

Chỉ dùng local. Trên Azure đổi mật khẩu ngay.

## Không ghép file RBAC cũ

`rbac/001_Drop_UQ_KhachHang_MaUser.sql` bỏ qua: schema hiện không tạo `UQ_KhachHang_MaUser`. Database cũ nào còn constraint đó thì chạy file đó một lần, riêng.
