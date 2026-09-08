# WAVV Customer UI — TourDuLich

Đây là bản dựng lại giao diện Customer dựa trên `wavv-main`, hiện chạy độc lập
với backend bằng dữ liệu mô phỏng trong `src/mockData.js`. Chưa có request API,
JWT hay kết nối database ở giai đoạn thiết kế giao diện này.

## Chạy local

```powershell
cd D:\DoAn\DoAn_TourDuLich\wavv-main\client
npm.cmd install
npm.cmd run dev
```

Mở URL Vite hiển thị trong terminal, thường là `http://localhost:5173`.

## Các luồng giao diện đã có

- Landing, danh sách tour, tìm kiếm, lọc khu vực/ngân sách và chi tiết tour.
- Đăng nhập/đăng ký mô phỏng, route bảo vệ và đăng xuất.
- Gợi ý cá nhân hóa mô phỏng.
- Tự thiết kế tour, gửi yêu cầu và chọn phương án tiết kiệm/cân bằng/cao cấp.
- Tạo booking, xem booking, thanh toán mô phỏng và xem hợp đồng placeholder.
- Hồ sơ cá nhân, khu vực giấy tờ hành khách, ưu đãi và danh sách yêu thích.
- Đánh giá tour hiển thị trong trang chi tiết.

Trạng thái, tài khoản demo, yêu thích và booking được lưu trong `localStorage`.
Sau khi duyệt UI, thay các thao tác mock bằng API client của backend ở giai đoạn
tích hợp kế tiếp.
