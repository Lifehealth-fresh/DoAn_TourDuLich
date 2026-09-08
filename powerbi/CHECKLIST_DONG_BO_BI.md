# Checklist đồng bộ Power BI

SQL Server là nguồn chân lý. Dùng các view trong `database/schema/005_ViewsBI.sql`; không tự tính lại số tiền còn lại hoặc doanh thu bằng DAX từ dữ liệu thô.

- Doanh thu: dùng `vw_DoanhThuTheoTour.TongThanhTien`, chỉ cộng thanh toán `DaXacNhan`/tương thích ngược `ThanhCong`.
- Công nợ: dùng `vw_ThanhToanTheoBooking.DaThanhToan`, `DangCho`, `ConLai`; dòng `ChoXacNhan` không phải doanh thu.
- Tỷ lệ hủy: dùng `vw_TyLeHuyBooking.TyLeHuy`.
- Điểm đánh giá: dùng `vw_DanhGiaTrungBinhTour`.
- Tỷ lệ lấp đầy và chuyển đổi gợi ý chưa có view chuẩn; chỉ bổ sung sau khi thống nhất mẫu số nghiệp vụ với backend.
- Dữ liệu đối tác là dữ liệu mock/giả lập nếu chưa tích hợp đối tác thật; gắn disclaimer trên dashboard.
- Khi refresh dashboard, kiểm tra `GET /api/ai-jobs/gan-nhat` bằng tài khoản Admin và tuổi `latestRecommendationAgeSeconds` của `/health`; SLA là không quá 420 giây.
