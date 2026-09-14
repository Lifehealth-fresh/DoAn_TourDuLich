# WAVV Customer UI — TourDuLich

Đây là website Customer React/Vite đã kết nối ASP.NET API bằng JWT. Đặt
`VITE_API_BASE_URL` theo `.env.example`; production phải trỏ tới URL API public.

## Chạy local

```powershell
cd D:\DoAn\DoAn_TourDuLich\wavv-main\client
npm.cmd install
npm.cmd run dev
```

Mở URL Vite hiển thị trong terminal, thường là `http://localhost:5173`.

## Các luồng giao diện đã có

- Landing, danh sách tour, tìm kiếm, lọc khu vực/ngân sách và chi tiết tour.
- Đăng nhập/đăng ký (SĐT 10 số, mật khẩu ≥ 8 ký tự gồm chữ và số). Tài khoản Admin/Sale bị chặn.
- Access token 2 giờ; client tự refresh. Đăng xuất thu hồi refresh token.
- Gợi ý cá nhân hóa (AI service + fallback).
- Tự thiết kế tour, gửi yêu cầu và chọn phương án tiết kiệm/cân bằng/cao cấp.
- Tạo booking, xem booking, thanh toán thủ công hoặc qua VNPay/MoMo sandbox và xem hợp đồng.
- Hồ sơ cá nhân, khu vực giấy tờ hành khách, ưu đãi và danh sách yêu thích.
- Đánh giá tour hiển thị trong trang chi tiết.

Token đăng nhập được lưu trong `localStorage`; dữ liệu nghiệp vụ được đọc/ghi qua API.
