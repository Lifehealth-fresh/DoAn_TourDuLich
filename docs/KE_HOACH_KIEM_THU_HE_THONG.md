# Kế hoạch kiểm thử hệ thống TourDuLich

> Cập nhật theo mã nguồn hiện có ngày 26/08/2026. Đây là checklist thực thi cho tester, không phải mô tả dự kiến. Một ca chỉ được đánh dấu **PASS** khi có bằng chứng; chưa chạy là **NOT RUN**; hành vi khác kỳ vọng là **FAIL**; chức năng chưa có giao diện là **N/A UI** nhưng API vẫn phải được kiểm thử.

## 1. Phạm vi và nguyên tắc

Phạm vi gồm API ASP.NET Core, SQL Server schema `001`–`012`, AI FastAPI, Cloudinary media tour, frontend customer, frontend admin và dữ liệu phục vụ Power BI. Mỗi API phải được kiểm tra cả đường thành công, dữ liệu sai, bản ghi không tồn tại, không có token (401), và token đúng nhưng sai vai trò/chủ sở hữu (403), nếu route có xác thực.

Không dùng dữ liệu thật hoặc ảnh/video thật của khách hàng. Với dữ liệu tạo mới, dùng hậu tố duy nhất, ví dụ `QA-20260826-001`, để dễ truy vết và dọn sau test. Không xóa dữ liệu seed.

Các vai trò dùng trong checklist:

| Ký hiệu | Tài khoản seed | Quyền chính |
|---|---|---|
| Public | Không token | Xem danh mục công khai |
| Customer | `0900000003` / `Test@123456` | Dữ liệu của chính mình |
| Sale | `0900000002` / `Test@123456` | Vận hành tour, booking, thanh toán, yêu cầu thiết kế |
| Admin | `0900000001` / `Test@123456` | Quản trị và toàn bộ quyền Sale |

> Chỉ dùng các tài khoản trên cho database local được tạo từ `003_SeedTestData.sql`. Nếu seed bị thay đổi, lấy tài khoản từ script seed, không tự đặt mật khẩu.

## 2. Chuẩn bị môi trường và bằng chứng

| ID | Bước | Thao tác | Kết quả mong đợi / bằng chứng |
|---|---|---|---|
| ENV-01 | Database | Tạo database trống, chạy lần lượt `database/schema/001` đến `012`. | Mỗi script hoàn tất không lỗi. Chụp tab Messages và lưu tên database/giờ chạy. |
| ENV-02 | Schema media | Sau schema `008`, kiểm tra `dbo.AnhTour` có `ImageURL`, `LoaiMedia`, `Url`. Sau schema `012`, có `CloudPublicId`, `CloudResourceType`. | Không có lỗi `Invalid column name`. Lưu kết quả truy vấn cột. |
| ENV-03 | Build backend | Tại `backend`, chạy `dotnet build TourDuLich.slnx`. | `0 Error`; lưu log build. |
| ENV-04 | AI | Tại `ai-service`, khởi động Uvicorn; mở `http://127.0.0.1:8000/health`. | JSON có `status: "ok"`, `database: "reachable"`. |
| ENV-05 | Backend | Khởi động API; mở `https://localhost:7290/swagger`. | Swagger tải được. `https://localhost:7290/` trả 401 là **đúng thiết kế** vì fallback authorization; không coi là lỗi UI. |
| ENV-06 | Customer | Chạy customer frontend và mở `http://localhost:5173`. | Trang công khai hiển thị, DevTools Console không có lỗi runtime đỏ. |
| ENV-07 | Admin | Chạy admin frontend và mở cổng cấu hình của frontend admin (thường là `5174`). | Trang đăng nhập/ứng dụng hiển thị; chưa kiểm thử thiết kế lại giao diện admin trong đợt này. |
| ENV-08 | Cloudinary | Thiết lập `CLOUDINARY_URL` ngoài mã nguồn (User Secrets hoặc biến môi trường), restart API. | Upload media không báo thiếu cấu hình; không lưu API secret vào Git, ảnh chụp, báo cáo. |

Lệnh kiểm chứng SQL chỉ đọc:

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AnhTour'
ORDER BY ORDINAL_POSITION;

SELECT TOP (20) *
FROM dbo.JobRunLog
ORDER BY BatDauLuc DESC;
```

## 3. Mẫu ghi nhận cho từng ca

Ghi một dòng cho từng lần chạy: `ID | ngày giờ | môi trường | người chạy | dữ liệu | actual result | PASS/FAIL/NOT RUN | request/response hoặc ảnh/log`.

Với API, luôn lưu: URL, method, payload đã che token/secret, HTTP status, body phản hồi, `Ma...` bản ghi tạo được. Với UI, luôn lưu URL, tài khoản/role, ảnh màn hình và Console/Network khi lỗi.

## 4. Danh mục field đầu vào theo DTO thực tế

Trường có dấu `?` là nullable trong DTO; các trường còn lại phải thử đủ giá trị hợp lệ, rỗng/null, sai kiểu và biên dữ liệu theo schema/validation.

| Nhóm | DTO / field |
|---|---|
| Auth/Admin | `RegisterDto(SoDienThoai, MatKhau)`; `LoginDto(SoDienThoai, MatKhau)`; `AdminCreateSaleDto(SoDienThoai, MatKhau)`; `AdminChangeRoleDto(TenVaiTro)` |
| Tour | `TourCreateDto(MaTour, TenTour, Mota?, ThoiGian?, DieuKhoan?, GiaTour, Slkhach, SlhuongDanVien?, LoaiTour, TrangThai?)`; `TourUpdateDto(TenTour, Mota?, ThoiGian?, DieuKhoan?, GiaTour, Slkhach, SlhuongDanVien?, LoaiTour, TrangThai?)` |
| Lịch | `LichKhoiHanhCreateDto(MaKhoiHanh, MaTour, NgayKhoiHanh?, NgayKetThuc?, DiaDiem?)`; `LichKhoiHanhUpdateDto(NgayKhoiHanh?, NgayKetThuc?, DiaDiem?)`; `LichTrinhCreateDto(MaTour, NgayThu, ThuTuTrongNgay, MaDthamQuan?, MaSanPham?, SoLuong, Mota?)`; `LichTrinhUpdateDto(MaDthamQuan?, MaSanPham?, SoLuong, Mota?)`; `SuaLichTrinhDto(ChiTiets[])` với mỗi chi tiết `(NgayThu, ThuTuTrongNgay, MaDthamQuan?, MaSanPham?, SoLuong, Mota?)` |
| Danh mục | `KhuVucCreateDto(MaKhuVuc, TenKhuVuc?, QuocGia?, ViDo?, KinhDo?, MuiGio?, TrangThai?)`; `KhuVucUpdateDto(TenKhuVuc?, QuocGia?, ViDo?, KinhDo?, MuiGio?, TrangThai?)`; `DiemThamQuanCreateDto(MaDthamQuan, TenDiaDanh?, DiaChi?, MaKhuVuc?, KinhDo?, ViDo?, Mota?)`; `DiemThamQuanUpdateDto(TenDiaDanh?, DiaChi?, MaKhuVuc?, KinhDo?, ViDo?, Mota?)` |
| Đối tác/sản phẩm | `DoiTacCreateDto(MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe?, SoDienThoai?, Email?, MaKhuVuc?, PhanTramHoaHong?, TrangThai?)`; `DoiTacUpdateDto(TenDoiTac, LoaiDoiTac, NguoiLienHe?, SoDienThoai?, Email?, MaKhuVuc?, PhanTramHoaHong?, TrangThai?)`; `SanPhamDoiTacCreateDto(MaSanPham, MaDoiTac, TenSanPham, DonViTinh?, GiaNiemYet, MaDthamQuan?, Mota?, TrangThai?)`; `SanPhamDoiTacUpdateDto(MaDoiTac, TenSanPham, DonViTinh?, GiaNiemYet, MaDthamQuan?, Mota?, TrangThai?)` |
| Media tour | `AnhTourUploadDto(File, ThuTu?, IsAvatar?)`; DTO URL cũ `AnhTourCreateDto(MaTour, Url, LoaiMedia, ThuTu?, IsAvatar?)` và `AnhTourUpdateDto(Url, LoaiMedia, ThuTu?, IsAvatar?)` phải nhận 410 Gone, không dùng để tạo media mới. |
| Customer | `KhachHangCreateDto(Ho, Ten, HoGiayTo?, TenGiayTo?, QuocTich?, DanhXung?, GioiTinh?, NgaySinh?, Email?)`; `KhachHangUpdateDto` cùng field; `GiayToCreateDto(LoaiGiayTo, SoTrenGiayTo, NgayCap, NgayHetHan, NoiCap)`; `GiayToUpdateDto` cùng field |
| Booking/hợp đồng | `DatDichVuCreateDto(MaTour, MaKhoiHanh, MaKhachHang?, SlnguoiLon, SltreEm)`; `DatDichVuTrangThaiDto(TrangThai)`; `HopDongCreateDto(MaBooking, SoHopDong?, DieuKhoanCamKet?)` |
| Thanh toán/khuyến mãi | `ThanhToanCreateDto(MaBooking, SoTien, PhuongThuc, LoaiThanhToan)`; `KhuyenMaiCreateDto(TenKm, MaCode, NgayBd, NgayKt, DonVi, GiamGia, CoCongDon, MaNhomKm?)`; `KhuyenMaiUpdateDto(TenKm?, MaCode?, NgayBd?, NgayKt?, DonVi?, GiamGia?, CoCongDon?, MaNhomKm?, TrangThai?)`; `DieuKienKmCreateDto(DonToiThieu?, LanDatDau?, SoLuong?)`; `KhuyenMaiApDungDto(MaBooking, MaCode)` |
| Đánh giá/hành vi | Tour `(MaTour, SaoDanhGia, NhanXet?, MediaUrls?)`; HDV `(MaHdv, SaoDanhGia, NhanXet?, MediaUrls?)`; sản phẩm `(MaSanPham, SaoDanhGia, NhanXet?, MediaUrls?)`; update Tour `(SaoDanhGia, NhanXet?, MediaUrls?)`; mỗi `MediaItemDto(Url, LoaiMedia)`; `HanhViCreateDto(MaTour, HanhDong)`; yêu thích `(MaTour)` |
| Thiết kế/AI | `YeuCauThietKeCreateDto(DiemDenMongMuon?, NgayDuKienDi?, SoNgay?, SoNguoiLon, SoTreEm, NganSachDuKien?, SoThichGhiChu?, MaGoiYThamKhao?, LyDoTuChoiGoiY?)`; `TuThietKeRequestDto(MaYeuCau)`; lý do từ chối `(LyDoTuChoi)`; `AiGoiYCreateDto(MaUser?, SoLuong=5, Alpha=0.5)` |

## 5. Ca kiểm thử API và nghiệp vụ

### 5.1 Auth, RBAC, quyền sở hữu

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| AUTH-01 | Đăng ký customer với `SoDienThoai`, `MatKhau` hợp lệ; login bằng thông tin đó. | Tạo user/customer và login trả JWT hợp lệ. |
| AUTH-02 | Đăng ký lại số đã tồn tại; login sai mật khẩu/số không tồn tại; bỏ từng field. | Bị từ chối, không lộ hash/mật khẩu. |
| AUTH-03 | Gọi toàn bộ route công khai: tour, lịch khởi hành, lịch trình, ảnh tour, khu vực, điểm tham quan, đối tác, sản phẩm, khuyến mãi, đánh giá. | 200 không cần token. |
| AUTH-04 | Với mỗi route customer/Sale/Admin, gọi không token; token customer; token Sale; token Admin. | 401 khi không token; 403 khi role không được phép; role hợp lệ thực hiện được. |
| AUTH-05 | Customer A dùng `MaBooking`, yêu cầu thiết kế, hồ sơ, giấy tờ, đánh giá của Customer B. | Không đọc/sửa/hủy/đánh giá thay người khác. |
| AUTH-06 | Admin tạo Sale, đổi role qua `/api/Admin`. | Chỉ Admin làm được; role mới có hiệu lực sau login/token mới. |

### 5.2 Danh mục và vận hành tour

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| CAT-01 | CRUD `KhuVuc`, `DiemThamQuan`, `DoiTac`, `SanPhamDoiTac` bằng Sale/Admin; thử mã trùng và FK không tồn tại. | Tạo/sửa/xóa hợp lệ; mã trùng/FK sai bị từ chối, dữ liệu không dở dang. |
| CAT-02 | Public đọc danh sách/chi tiết các danh mục; thử ID không tồn tại. | Chỉ thấy dữ liệu được API công bố; ID sai là lỗi phù hợp. |
| TOUR-01 | Sale/Admin tạo tour đủ field; đọc danh sách, chi tiết, lịch khởi hành, lịch trình, media. | Giá trị trả về khớp dữ liệu đã tạo và liên kết đúng tour. |
| TOUR-02 | Tạo/sửa/xóa lịch khởi hành và lịch trình; thử tour/điểm/sản phẩm không tồn tại, ngày/kỳ tự không hợp lệ, số lượng biên. | Ràng buộc dữ liệu được áp dụng; không tạo lịch mồ côi/trùng logic. |
| TOUR-03 | Tạo booking, hợp đồng `DaKy`; sau đó Sale/Admin đổi `GiaTour` hoặc `DieuKhoan` tour. | Bị chặn vì có hợp đồng đã ký. Cập nhật các field không bị khóa phải được kiểm tra riêng. |
| TOUR-04 | Đọc `/api/Tour/{maTour}/lich-khoi-hanh`, `/lich-trinh`, `/anh` cho tour có và không có dữ liệu. | 200, mảng rỗng khi chưa có dữ liệu; không 500. |

### 5.3 Media tour / Cloudinary

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| MEDIA-01 | Admin/Sale upload JPEG, JPG, PNG, WebP hợp lệ: mỗi file <= 10 MB. | Upload public Cloudinary thành công; DB lưu URL, public ID, resource type; `GET /api/AnhTour/theo-tour/{maTour}` hiển thị được. |
| MEDIA-02 | Upload MP4/WebM hợp lệ: mỗi file <= 100 MB. | Tương tự MEDIA-01 với resource type video. |
| MEDIA-03 | Thử ảnh/video vượt giới hạn, đuôi sai, MIME client giả, file chữ đổi đuôi ảnh/video, file có signature sai. | Bị từ chối trước khi upload Cloudinary; không có bản ghi mồ côi. |
| MEDIA-04 | Thử customer và public gọi upload/thay/xóa. | 401/403; chỉ Sale/Admin có quyền. |
| MEDIA-05 | Thay media bằng file hợp lệ rồi xóa; kiểm tra GET và Cloudinary/DB. | URL/bản ghi mới đúng; file cũ hoặc file xóa không còn được tham chiếu. |
| MEDIA-06 | Gọi POST/PUT URL cũ `/api/AnhTour`. | 410 Gone, hướng dẫn dùng upload file; không chấp nhận URL do client đưa vào. |
| MEDIA-07 | Đặt `IsAvatar` cho nhiều media cùng tour, sắp thứ tự `ThuTu`, bỏ `ThuTu`. | Kiểm tra đúng quy tắc đang triển khai và danh sách trả về ổn định; ghi FAIL nếu xuất hiện nhiều avatar trái quy tắc hệ thống. |

### 5.4 Booking, sức chứa và hủy

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| BOOK-01 | Customer tạo booking tour/lịch hợp lệ, số người lớn/trẻ em hợp lệ; đọc `/cua-toi` và chi tiết. | Booking thuộc đúng customer, tổng/snapshot dữ liệu đúng. |
| BOOK-02 | Thử tour/lịch không tồn tại, lịch không thuộc tour, số lượng 0/âm, thiếu field, customer A truyền customer B. | Từ chối; không trừ chỗ. |
| BOOK-03 | Tạo 2 request đồng thời để tổng số khách vượt đúng 1 chỗ còn lại. | Chỉ một request thành công; không oversell. Lưu thời điểm, payload, hai response và số chỗ sau test. |
| BOOK-04 | Customer hủy booking của mình; thử hủy booking không thuộc mình và hủy lặp. | Hủy đúng trạng thái; không hủy thay người khác; trạng thái/chỗ được xử lý nhất quán. |
| BOOK-05 | Sale/Admin cập nhật trạng thái qua route quản trị; thử state không hợp lệ. | Chỉ transition được mã nguồn cho phép; customer không gọi được. |

### 5.5 Khuyến mãi, hợp đồng, thanh toán

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| PROMO-01 | Sale/Admin CRUD khuyến mãi và điều kiện; thử mã trùng, thời gian ngược, giảm giá/đơn vị sai. | Chỉ dữ liệu hợp lệ được lưu. |
| PROMO-02 | Customer áp mã hợp lệ lên booking của mình; thử hết hạn, chưa đến ngày, điều kiện không đạt, booking người khác, áp lặp/cộng dồn. | Tổng tiền và trạng thái đúng theo điều kiện/`CoCongDon`; không làm thay booking khác. |
| CONTRACT-01 | Sale/Admin tạo hợp đồng cho booking; customer đọc hợp đồng của mình, customer khác thử đọc. | Hợp đồng/snapshot tồn tại, chỉ chủ sở hữu hoặc role vận hành được xem. |
| CONTRACT-02 | Ký hợp đồng và kiểm tra snapshot điều khoản/giá sau khi tour bị thử cập nhật. | Hợp đồng đã ký giữ snapshot, tour bị chặn đổi giá/điều khoản. |
| PAY-01 | Customer tạo thanh toán bằng `PhuongThuc=ChuyenKhoan`, `LoaiThanhToan` hợp lệ. | Bản ghi bắt đầu `ChoXacNhan`, không tự `ThanhCong`/`DaXacNhan`. |
| PAY-02 | Customer tạo thanh toán `TienMat`. | Bắt đầu `ChoXacNhan`; có thể chờ thu tại quầy/khách sạn theo nghiệp vụ mock. |
| PAY-03 | Thử `PhuongThuc=MoPhong`, giá trị khác `ChuyenKhoan/TienMat`, tiền <=0, quá `conLaiKhaDung`, booking không phải của mình. | Bị từ chối; không phát sinh dòng thanh toán sai. |
| PAY-04 | Tạo nhiều dòng đang chờ sao cho tổng vượt số dư. | Hệ thống tính cả `ChoXacNhan` và chặn vượt. |
| PAY-05 | Sale/Admin xác nhận và từ chối từng dòng; customer thử gọi hai endpoint này. | Chỉ Sale/Admin xử lý. Tổng hợp trả `daThanhToan`, `dangCho`, `conLai`, `conLaiKhaDung` đúng. |
| PAY-06 | Gửi lặp cùng yêu cầu/idempotency key (nếu client gửi header này); gửi lại sau timeout. | Không tạo hai giao dịch cho cùng yêu cầu nghiệp vụ. Lưu header và số dòng DB. |
| PAY-07 | Booking đã hủy hoặc thanh toán đã xử lý: tạo/xác nhận/từ chối lặp. | Không cho trạng thái mâu thuẫn; không thay đổi tổng sai. |

### 5.6 Hồ sơ, giấy tờ, đánh giá, yêu thích, hành vi, thông báo

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| CUS-01 | Customer tạo/sửa hồ sơ, CRUD giấy tờ; test ngày cấp/hết hạn và field bắt buộc. | Chỉ chủ hồ sơ thao tác; dữ liệu ngày hợp lý và không lộ cho user khác. |
| CUS-02 | Sale tìm/tạo/sửa customer và giấy tờ theo các route quản trị. | Sale/Admin được phép trong phạm vi controller; customer không có quyền quản trị. |
| REVIEW-01 | Customer đủ điều kiện tạo review Tour/HDV/sản phẩm, có/không media URL; public đọc danh sách. | Review hiển thị ngay, không có bước duyệt. |
| REVIEW-02 | Customer chưa đủ điều kiện, review trùng, sửa review của người khác, sao ngoài biên, media type/URL sai. | Bị từ chối; không phát sinh review không hợp lệ. |
| REVIEW-03 | Customer sửa review tour của mình. | Chỉ review thuộc mình được thay đổi; danh sách public phản ánh nội dung mới. |
| FAV-01 | Customer thêm/list/xóa tour yêu thích; thêm trùng, tour không tồn tại, xóa mục người khác. | Không trùng, chỉ owner thao tác. |
| BEH-01 | Customer ghi hành vi có `MaTour`, `HanhDong`; đọc hành vi của mình. | Chỉ ghi/đọc hành vi của chính user; dữ liệu được AI dùng đúng user. |
| NOTI-01 | Đọc thông báo của mình, số chưa đọc, đánh dấu một/tất cả đã đọc. | Số đếm và trạng thái cập nhật đúng, không ảnh hưởng user khác. |

### 5.7 Yêu cầu thiết kế tour, báo giá và AI

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| DESIGN-01 | Customer tạo yêu cầu thiết kế với toàn bộ field; list/detail yêu cầu của mình. | Yêu cầu thuộc customer, trạng thái ban đầu đúng. |
| DESIGN-02 | Customer chọn gợi ý AI hoặc từ chối kèm `LyDoTuChoiGoiY`; không tạo yêu cầu mới sau từ chối. | Từ chối gợi ý **không bắt buộc** tạo `YeuCauThietKe`; lưu/hiển thị đúng theo luồng hiện có. |
| DESIGN-03 | Sale/Admin list, tạo lịch trình, sửa lịch trình, gửi báo giá; customer duyệt hoặc hủy; Sale/Admin từ chối. | Chỉ cho phép chuyển trạng thái theo state machine; ghi FAIL nếu cho phép bỏ qua trạng thái. |
| DESIGN-04 | Tạo tour từ yêu cầu qua `/api/Tour/tu-thiet-ke`; kiểm tra công thức giá ở `docs/GIA-TOUR-TU-THIET-KE.md`. | Giá trả/lưu phải bằng công thức hiện hành, không lấy số do client tự áp đặt. |
| AI-01 | Gọi `POST /api/AiGoiY/sinh-goi-y` khi AI đang chạy; test `SoLuong`, `Alpha`, `MaUser` là owner/khác owner. | Gợi ý thuộc user phù hợp; role/ownership đúng. |
| AI-02 | Lấy `/api/AiGoiY/cua-toi`; so khớp với hành vi/booking đã tạo. | Không lộ gợi ý user khác; không lỗi khi dữ liệu thưa. |
| AI-03 | Dừng AI service có chủ đích rồi gọi backend; khởi động lại và gọi tiếp. | Backend trả lỗi có kiểm soát, không treo/500 vô nghĩa; sau khi chạy lại phục hồi. |
| AI-04 | Kiểm tra `/health`, `GET /api/ai-jobs/gan-nhat` với Admin, và `JobRunLog`. | Job chạy trong SLA tối đa 7 phút (`latestRecommendationAgeSeconds <= 420` khi hệ thống ổn định); log có bắt đầu/kết thúc/trạng thái/thông tin lỗi khi có lỗi. Customer/Sale không được xem endpoint Admin. |

### 5.8 Phân trang, lỗi chung và Power BI

| ID | Bước test | Kết quả mong đợi |
|---|---|---|
| API-01 | Với các list hỗ trợ `page/pageSize` hoặc query tương ứng: trang đầu, trang sau, pageSize 1, biên lớn, 0/âm/chữ. | Kết quả ổn định, tổng/phân trang đúng, input sai bị xử lý không 500. |
| API-02 | Gọi từng route bằng ID/mã không tồn tại, JSON lỗi, field thừa, Content-Type sai. | 400/404 phù hợp, response không chứa stack trace/secret. |
| API-03 | Kiểm tra 21 controller trong Swagger theo từng method, đối chiếu với bảng này. | Không có route nào bị bỏ qua; thêm dòng test nếu Swagger có action mới. |
| BI-01 | Mở dataset/Power BI bằng user có quyền DB chỉ đọc; kiểm tra số booking, doanh thu đã xác nhận, thanh toán chờ, đánh giá, hành vi, recommendation và JobRunLog. | Số dashboard đối chiếu được với truy vấn SQL/API ở cùng thời điểm; không dùng thanh toán bị từ chối làm doanh thu. |
| BI-02 | Tạo một booking/thanh toán/AI job test rồi refresh dashboard. | Số liệu thay đổi đúng nguồn, đúng kỳ thời gian; không trùng do idempotency. |

## 6. Kịch bản frontend customer end-to-end

Chạy mỗi kịch bản trên Chrome/Edge ở kích thước desktop và mobile. Mở DevTools Console + Network; mọi request 4xx/5xx phải được ghi vào evidence, trừ response dự kiến như redirect login hoặc validation.

| ID | Bước | Kết quả mong đợi |
|---|---|---|
| CUST-UI-01 | Mở trang chủ, danh sách/chi tiết tour, tìm/lọc, lịch khởi hành, lịch trình, ảnh/video. | Nội dung tải, link/nút hoạt động, trạng thái loading/empty/error dễ hiểu, media Cloudinary hiển thị. |
| CUST-UI-02 | Mở trực tiếp `/tu-thiet-ke`, `/goi-y`, `/booking`, `/booking/{id}`, `/ho-so` khi chưa login. | Chuyển tới `/dang-nhap`; không trắng trang. |
| CUST-UI-03 | Login customer, quay lại các URL trên; tạo yêu cầu thiết kế, xem/chọn/từ chối gợi ý, xem hồ sơ. | Màn hình render và request dùng token đúng; không có lỗi Console. |
| CUST-UI-04 | Chọn lịch còn chỗ, tạo booking, xem chi tiết, áp khuyến mãi, xem hợp đồng/thông báo/yêu thích/review. | UI phản ánh response API và báo lỗi validation rõ ràng. |
| CUST-PAY-001 | Từ trang chi tiết booking, chọn thanh toán. Kiểm tra payload Network. | **Lỗi đã xác nhận, trạng thái hiện tại FAIL:** frontend đang gửi `PhuongThuc: "MoPhong"`, trong khi API chỉ nhận `ChuyenKhoan` hoặc `TienMat`; thao tác bị backend từ chối. Không đánh dấu PASS cho đến khi frontend gửi đúng hai lựa chọn và hiển thị `ChoXacNhan`. |
| CUST-UI-05 | Logout, refresh, mở lại link bảo vệ. | Token bị xóa; quay về login, không lộ dữ liệu cache của user cũ. |
| CUST-UI-06 | Khi gặp trắng trang: lưu URL, Console, Network (request lỗi đầu tiên), terminal Vite và response API. | Không kết luận nguyên nhân chỉ từ ảnh; tái hiện được trước khi sửa. |

## 7. Kịch bản frontend admin cơ bản

Giao diện admin chưa thuộc phạm vi chỉnh sửa hiện tại, nhưng vẫn cần kiểm thử nghiệp vụ sau bằng tài khoản Admin/Sale seed:

| ID | Bước | Kết quả mong đợi |
|---|---|---|
| ADM-UI-01 | Login Admin và Sale bằng tài khoản seed. | Admin vào màn quản trị; Sale chỉ thấy/hành động trong quyền được cấp. |
| ADM-UI-02 | Quản lý tour/lịch trình/lịch khởi hành/media, booking, hợp đồng, thanh toán, khuyến mãi, đối tác/sản phẩm, yêu cầu thiết kế. | Các thao tác UI tạo đúng request API và hiện thông báo thành công/lỗi. |
| ADM-UI-03 | Admin mở dashboard/AI jobs. | Chỉ Admin xem được JobRunLog và số liệu; số liệu đối chiếu BI/API. |

## 8. Regression tự động và tiêu chí nghiệm thu

Sau mỗi sửa đổi backend, chạy:

```powershell
cd D:\DoAn\DoAn_TourDuLich\backend
dotnet build TourDuLich.slnx
dotnet test .\tests\TourDuLich.IntegrationTests\TourDuLich.IntegrationTests.csproj
```

Bộ integration test hiện có phải được chạy và lưu kết quả: `AuthTests`, `AuthorizationTests`, `RbacTests`, `SensitiveEndpointRbacTests`, `OwnershipTests`, `BookingTests`, `CancellationTests`, `HopDongTests`, `PaymentTests`, `PromotionTests`, `MediaAuthorizationTests`, `AiGoiYTests`, `SelfDesignedTourFlowTests`, `SelfDesignedTourPrivacyTests`, `TuThietKeTourPricingTests`, `YeuCauThietKeStateMachineTests`.

Sau mỗi sửa AI, restart Uvicorn và kiểm tra `/health`, rồi gọi một gợi ý end-to-end. Sau mỗi sửa database, chạy schema trên database trống trước, sau đó chạy smoke test ENV-02, AUTH-01, TOUR-01, BOOK-01, PAY-01, AI-04.

Chỉ nghiệm thu khi:

1. ENV-01 đến ENV-08 PASS (ngoại lệ cấu hình Cloudinary có thể ghi BLOCKED khi chưa có secret hợp lệ).
2. Không có FAIL mức chặn ở Auth/RBAC/ownership, booking sức chứa, hợp đồng snapshot, thanh toán, media validation, state machine hoặc AI SLA.
3. CUST-PAY-001 được chuyển PASS sau khi đã sửa và kiểm thử lại; lỗi trắng trang chỉ được đóng khi có bằng chứng tái hiện rồi hết tái hiện.
4. Build và toàn bộ integration test PASS trên máy dev có SQL Server phù hợp.
5. Báo cáo cuối đính kèm log, request/response đã che bí mật, ảnh UI và danh sách dữ liệu test cần dọn.

## 9. Bảng tổng kết chạy test

| Nhóm | Tổng ca | PASS | FAIL | BLOCKED | NOT RUN | Link bằng chứng |
|---|---:|---:|---:|---:|---:|---|
| Môi trường/schema | 8 |  |  |  |  |  |
| Auth/RBAC | 6 |  |  |  |  |  |
| Danh mục/tour/media | 11 |  |  |  |  |  |
| Booking/khuyến mãi/hợp đồng/thanh toán | 15 |  |  |  |  |  |
| Customer/review/hành vi/thông báo | 6 |  |  |  |  |  |
| Thiết kế tour/AI/BI | 8 |  |  |  |  |  |
| Customer UI | 6 |  |  |  |  |  |
| Admin UI | 3 |  |  |  |  |  |