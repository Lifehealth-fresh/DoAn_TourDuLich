# Tự thiết kế: trợ lý chat + lập lịch ràng buộc

## Việc đã làm

- Chat khách **chỉ** cho tự thiết kế. Khách **chọn tỉnh** (gõ `nha trang`, `Da Lat`, `sai gon` vẫn ra đúng tỉnh).
- Không gọi Google Maps / Gemini. Ghép lịch từ điểm, KS, nhà hàng **cùng tỉnh** trong CSDL (018: đủ 63 tỉnh).
- 3 phương án: tiết kiệm / cân bằng / cao cấp quanh ngân sách ±12%.
- Thời gian đi: bảng `MatranDiChuyen` (cặp hay demo) hoặc **Haversine tọa độ 63 tỉnh** (xe / tàu / máy bay). Tới sau 20:00 chỉ check-in. Ngày về tính giờ có mặt lại điểm xuất phát.
- Admin: ô tìm theo từ khóa; lọc **cùng MaTinh**.

## SQL (Azure, đúng database đang dùng)

1. `018_TinhThanh_SampleCatalog.sql` — 63 tỉnh, mỗi tỉnh 5 điểm + 5 vui chơi + 5 quán + 4–5 KS.
2. `023_TuThietKeChatPlanner.sql` — chat, alias, ma trận.
3. `024_MatchToanBoOffline.sql` — alias đủ 63 tỉnh + thành phố (Nha Trang, Hội An, Phú Quốc…), backfill `MaTinh` điểm cũ 014/017.

Không xóa vé/user. Không cần API key Google.

## API

- `GET /api/TinhThanh?q=nha`
- `POST /api/YeuCauThietKe/chat`
- `GET /api/YeuCauThietKe/chat/{id}`
