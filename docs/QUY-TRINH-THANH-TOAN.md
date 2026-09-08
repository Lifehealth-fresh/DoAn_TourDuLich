# QUY TRÌNH NGHIỆP VỤ THANH TOÁN — HỆ THỐNG TOUR DU LỊCH

> Trạng thái thanh toán trong code sau Prompt 4: `ChoXacNhan → DaXacNhan | TuChoi`, dữ liệu cũ `ThanhCong` được tính như `DaXacNhan`. Tổng hợp tiền: `daThanhToan = SUM(SoTien WHERE TrangThai IN (DaXacNhan, ThanhCong))`, `dangCho = SUM(SoTien WHERE ChoXacNhan)`, `conLai = ThanhTien - daThanhToan`, `conLaiKhaDung = ThanhTien - daThanhToan - dangCho`.

Trước khi chạy API hoặc integration test, áp dụng `database/schema/015_ThanhToan_PaymentGateway.sql` lên đúng database dev/test.

## Sơ đồ BPMN (mermaid)

```mermaid
flowchart TD
    A[Khách: Bấm Đặt dịch vụ<br/>tạo DatDichVu ChoXacNhan<br/>+ HopDong DuThao] --> B{Chọn phương thức?}
    B -->|Chuyển khoản<br/>DatCoc / ThanhToanConLai / ThanhToanDu| C[POST /api/ThanhToan<br/>PhuongThuc=ChuyenKhoan<br/>Tạo ThanhToan ChoXacNhan]
    B -->|Tiền mặt<br/>thu tại quầy / khách sạn| D[POST /api/ThanhToan<br/>PhuongThuc=TienMat<br/>Tạo ThanhToan ChoXacNhan<br/>hẹn thu tại điểm]
    B -->|VNPay hoặc MoMo| P[POST /api/ThanhToan/tao-phien-cong<br/>Tạo ThanhToan ChoXacNhan<br/>lưu GatewayOrderId + PayUrl]
    P --> Q[Chuyển khách sang trang<br/>sandbox của cổng]
    Q --> R[Return URL: kiểm tra chữ ký<br/>chỉ điều hướng về Customer<br/>không ghi DaXacNhan]
    Q --> S[IPN server-to-server]
    S --> T{Chữ ký + order<br/>+ số tiền hợp lệ?}
    T -->|Không| V[HTTP 400 / mã lỗi<br/>không đổi DaXacNhan]
    T -->|Có và giao dịch thành công| U[IPN ghi DaXacNhan<br/>GatewayTxnId + PaidAt]
    U --> J
    C --> E[Sale: Đối soát sao kê<br/>kiểm tra số tiền thực nhận]
    D --> F[Sale: Thu tiền mặt tại<br/>điểm khởi hành / khách sạn<br/>có thể TRẢ SAU khi trải nghiệm<br/>dịch vụ lưu trú]
    E --> G{Số tiền khớp<br/>thỏa thuận?}
    F --> G
    G -->|Khớp| H[PUT /api/ThanhToan/maTt/xac-nhan<br/>Sale/Admin → DaXacNhan]
    G -->|Thiếu / sai| I[PUT /api/ThanhToan/maTt/tu-choi<br/>kèm lyDo — trở lại ChoXacNhan mới]
    H --> J{Tổng DaXacNhan<br/>đủ ThanhTien?}
    I --> K[Khách thanh toán bổ sung<br/>tạo dòng ChoXacNhan mới] --> E
    J -->|Chưa đủ| L[Chờ đợt tiếp theo<br/>conLaiKhaDung = ThanhTien - daThanhToan - dangCho]
    L --> C
    J -->|Đủ| M[DatDichVu: Sale chuyển<br/>DaXacNhan → DaThanhToan<br/>PUT /api/DatDichVu/maBooking/trang-thai<br/>đủ tiền mới cho qua]
    M --> N[HoanThanh sau khi<br/>kết thúc tour]
```

Nguồn file mermaid: `docs/QUY-TRINH-THANH-TOAN.md` (chính file này). Để có file `.bpmn` chuẩn Camunda, import mermaid trên vào https://mermaid.live rồi export, hoặc mở PR riêng.

## Quy tắc chi tiết

1. **Tạo thanh toán:** chỉ KhachHang sở hữu booking được `POST`; `SoTien > 0`; `LoaiThanhToan ∈ {DatCoc, ThanhToanConLai, ThanhToanDu}`; `PhuongThuc ∈ {ChuyenKhoan, TienMat}` dùng API cũ, còn `VNPay|MoMo` dùng `POST .../tao-phien-cong`; mọi nhánh đều chặn `SoTien > conLaiKhaDung`.
2. **Xác nhận/từ chối:** chỉ `Sale,Admin` được duyệt tay `TienMat|ChuyenKhoan`; chỉ dòng `ChoXacNhan` mới được chuyển; booking `DaHuy` không được xác nhận; xác nhận không được làm `daThanhToan + SoTien > ThanhTien`. Giao dịch `VNPay|MoMo` trả 400 nếu Sale/Admin cố duyệt tay.
3. **Nhiều đợt:** mỗi đợt là một dòng `ThanhToan` cùng `MaBooking`; không giới hạn số đợt; `GET .../tong-hop` trả cả `daThanhToan`, `dangCho`, `conLai`, `conLaiKhaDung` để FE hiển thị đúng.
4. **Tiền mặt trả sau:** với dịch vụ lưu trú/dịch vụ ngoài tour, cho phép tạo `ChoXacNhan` với `TienMat` và Sale xác nhận SAU khi khách đã trải nghiệm — không chặn theo thời gian tour, chỉ chặn khi booking đã `DaHuy`.
5. **Cổng thanh toán:** VNPay dùng HMAC-SHA512; MoMo dùng HMAC-SHA256. Return URL chỉ thông báo giao diện. IPN phải đúng chữ ký, đúng mã đơn và đúng số tiền mới ghi `DaXacNhan`, `GatewayTxnId`, `PaidAt`; kết quả cổng thất bại chuyển `TuChoi` để giải phóng số tiền đang chờ, còn chữ ký sai không đổi dữ liệu; gọi lại IPN không được ghi trùng.
6. **Phạm vi mock:** chỉ `TienMat|ChuyenKhoan` còn là luồng Sale đối soát thủ công. `VNPay|MoMo` là tích hợp sandbox và cần credential, Return/IPN URL HTTPS public do nhà cung cấp cấp/cấu hình.

## API liên quan sau sửa

- `POST /api/ThanhToan` → `201 { ChoXacNhan }`
- `POST /api/ThanhToan/tao-phien-cong` `[KhachHang]` → `{ maTt, phuongThuc, payUrl, trangThai }`
- `GET /api/ThanhToan/vnpay/return` và `GET .../vnpay/ipn` → verify HMAC-SHA512
- `GET /api/ThanhToan/momo/return` và `POST .../momo/ipn` → verify HMAC-SHA256
- `PUT /api/ThanhToan/{maTt}/xac-nhan` `[Sale,Admin]` → `DaXacNhan`
- `PUT /api/ThanhToan/{maTt}/tu-choi` `[Sale,Admin]` → `TuChoi`
- `GET /api/ThanhToan/theo-booking/{maBooking}/tong-hop` → `{ tongTien, daThanhToan, dangCho, conLai, conLaiKhaDung }`
