# Tự thiết kế (form) + chat hỗ trợ + LLM function calling

## Khách hàng thấy gì

- **Tự thiết kế = form**: tỉnh xuất phát, tỉnh đến (chọn từ 63 tỉnh), ngày, giờ, số ngày, NL/TE, ngân sách, mục đích, ghi chú.
- **Chat góc phải** chỉ hướng dẫn thao tác (đặt tour, thanh toán, hủy, cách dùng form). Không hỏi từng ô để sinh lịch.
- Submit form → API lưu `YeuCauThietKe` → backend ghép **3 lịch từ CSDL** (điểm/KS/nhà hàng cùng tỉnh).

## LLM (tùy chọn)

```
Khách gửi form
  → ASP.NET (quyền, validate)
  → Nếu có Llm:ApiKey: Gemini function calling
       search_sights / search_hotels / search_meals / calculate_route
       (tool chạy trên CSDL + Haversine, không Google Maps)
  → Sinh 3 lịch bằng DeXuatLichTrinhService (chỉ ID có trong CSDL)
  → Nếu không key / Gemini lỗi: cùng generator CSDL (đồ án vẫn chạy)
```

Azure App Settings:

- `Llm__ApiKey` = key **Google AI Studio** (Gemini), không phải Maps.
- `Llm__Model` = `gemini-2.0-flash`
- Để trống = không gọi Gemini.

## SQL

Đã có từ 023/024 (`MaTinhXuatPhat`, `GioKhoiHanh`, `HoiThoaiThietKe`…). Không script mới bắt buộc.

## API

- `POST /api/YeuCauThietKe` body: maTinhXuatPhat, maTinhDen, ngayDuKienDi, gioKhoiHanh, soNgay, soNguoiLon, soTreEm, nganSachDuKien, mucDich, soThichGhiChu
- `POST /api/HoTro/chat` — trợ lý thao tác
- `GET /api/TinhThanh?q=`

## Quy tắc

- Không bịa placeId / giá.
- Không Google Maps.
- Tới sau 20:00 chỉ check-in.
- 3 mức: tiết kiệm / cân bằng / cao cấp quanh ngân sách.
