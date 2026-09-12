# Nghiệm thu vòng sĩ số lịch, hồ sơ khách và duyệt tự thiết kế hai bước

Ngày kiểm tra: 2026-09-12. Nhánh: `develop`.

Repo đã sửa: `C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich`. Không sửa bản trên ổ D.

## 1. Phạm vi thay đổi

- Sức chứa thuộc **một LichKhoiHanh**, không thuộc dòng LichTrinh ngày 1/2/3. `SoCho ?? Tour.Slkhach`; cộng người lớn + trẻ em theo đúng MaKhoiHanh; loại DaHuy và ChoHoanTien. Số tài khoản là chỉ số riêng.
- CreateBooking giữ transaction Serializable, khóa Tour rồi LichKhoiHanh bằng UPDLOCK/HOLDLOCK. Cập nhật sức chứa cũng khóa cùng thứ tự và không cho giảm xuống dưới số chỗ đang giữ. Đổi trạng thái/hủy/xác nhận hoàn vé đọc trạng thái sau khi khóa trong giao dịch để không ghi đè vé vừa hủy bằng trạng thái cũ.
- Khách thấy lịch tương lai, sức chứa/đã đặt/còn trống; không chọn lịch hết chỗ; không gửi số người vượt chỗ. Tour không lịch có nút đặt disabled.
- Admin: Quản lý tour → Lịch khởi hành và khách → từng vé → hồ sơ/giấy tờ. Không thêm menu hồ sơ khách toàn cục.
- Lịch tự thiết kế: sửa/lưu → gửi khách → khách đồng ý → Admin duyệt. API và UI ngăn sửa trực tiếp lịch/giá khi đang chờ khách/chờ duyệt/đã duyệt.
- Dùng lại LyDoTuChoiBoiSale. Ghi `[Admin] ` hoặc `[KhachHang] `; JSON tách `lyDo`, `nguonLyDo`; không tiền tố xem như Admin. Giữ lý do khi sửa/lưu; chỉ xóa khi gửi duyệt mới hoặc duyệt thành công.
- Giữ hành vi duyệt có sẵn của bản C: tour tự thiết kế sau duyệt là HoatDong và được tạo lịch tương lai nếu chưa có. Không thay đổi công thức giá/hủy/hoàn tiền hiện có.

Không sửa JWT, CORS, cổng thanh toán, Cloudinary/upload, keep-alive, ai-service, Dockerfile, App Service, đánh giá, mã khuyến mãi hay ErrorBoundary. Không thêm thư viện.

## 2. SQL cần chạy trên Azure SQL

Database đang dùng phải có schema của bản C đến 015. **Không xóa database, không chạy lại 001 trên database có dữ liệu.**

Mở đúng database ứng dụng trong Azure SQL/SSMS, kiểm tra `SELECT DB_NAME();`, sau đó chạy [016_LichKhoiHanh_SoCho.sql](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/database/schema/016_LichKhoiHanh_SoCho.sql):

```sql
IF COL_LENGTH(N'dbo.LichKhoiHanh', N'SoCho') IS NULL
BEGIN
    ALTER TABLE dbo.LichKhoiHanh ADD SoCho INT NULL;
END;
GO
```

Đây là thay đổi schema duy nhất. Không thêm cột lý do hay trạng thái. ChoKhachXacNhan dùng cột TrangThai hiện có và PadTo20.

Kiểm tra sau khi chạy:

```sql
SELECT COL_LENGTH(N'dbo.LichKhoiHanh', N'SoCho') AS SoChoLength;
-- Kỳ vọng: 4
SELECT TOP (20) MaKhoiHanh, MaTour, SoCho
FROM dbo.LichKhoiHanh ORDER BY MaKhoiHanh;
-- Các lịch cũ giữ NULL và sử dụng Tour.Slkhach.
```

Chạy migration trước khi khởi động backend mới. Chưa thực thi migration vào database của bạn trong phiên này.

## 3. API để nghiệm thu

| API | Quyền / dữ liệu |
| --- | --- |
| GET /api/Tour/{id}/lich-khoi-hanh | Thêm soCho, sucChua, daDat, conTrong, soTaiKhoan. Khách chỉ thấy lịch tương lai; staff vẫn thấy lịch cũ để tra cứu. |
| GET /api/LichKhoiHanh và GET /api/LichKhoiHanh/{id} | Cùng công thức sĩ số; giữ phân trang của danh sách. |
| POST /api/LichKhoiHanh; PUT /api/LichKhoiHanh/{id} | Sale/Admin, nhận soCho nullable; số âm bị từ chối. NULL lấy sức chứa tour. |
| GET /api/LichKhoiHanh/{id}/khach | Sale/Admin; mỗi booking một dòng; header tổng số chỗ và tài khoản phân biệt. |
| GET /api/DatDichVu/{id}/ho-so-khach | Sale/Admin; ưu tiên MaKhachHang gắn vé, nếu chưa gắn thì tìm theo MaUser. Không có vé: 404. Có vé nhưng chưa có hồ sơ: các trường hồ sơ null, giayTo = []. |
| GET /api/YeuCauThietKe/{id}/lich-hien-tai | Chủ yêu cầu hoặc Sale/Admin; lịch LichTrinh đang lưu, giá Tour, trạng thái và lý do đã tách nguồn. Không dùng đề xuất heuristic thay lịch thực. |
| PUT /api/YeuCauThietKe/{id}/gui-duyet | Sale/Admin; DangThietKe/CanChinhSua → ChoKhachXacNhan, tour ChoXacNhan; xóa lý do. |
| PUT /api/YeuCauThietKe/{id}/dong-y-lich | Khách sở hữu yêu cầu; ChoKhachXacNhan → ChoDuyet. |
| PUT /api/YeuCauThietKe/{id}/yeu-cau-chinh-sua | Khách sở hữu; body `{"lyDo":"Thêm thời gian nghỉ"}`; → CanChinhSua, tour Nhap. |
| PUT /api/YeuCauThietKe/{id}/duyet; .../tu-choi-boi-sale | Chỉ khi ChoDuyet. Trạng thái chưa phù hợp: 409. |
| GET /api/Tour/{id} | Chủ yêu cầu xem được tour tự thiết kế chưa duyệt. Catalog mặc định không liệt kê tour tự thiết kế. |

Ví dụ một dòng vé: `soCho=4, slnguoiLon=3, sltreEm=1`. Không biến 4 người thành 1 chỗ chỉ vì cùng tài khoản.

## 4. Checklist nghiệm thu bằng giao diện và HTTP

Chuẩn bị riêng một tour test hoạt động, Slkhach=30; lịch D1 tương lai có SoCho=10, lịch D2 cùng tour có SoCho=2; ba khách A/B/C và staff. Mỗi lượt cần ghi request, status HTTP, response và kết quả GET lại, không chỉ dựa vào thông báo UI.

| Mã | Thao tác / dữ liệu | Kết quả phải kiểm chứng |
| --- | --- | --- |
| C01 | A đặt D1: 3 NL + 1 TE; B: 3 NL + 1 TE; C: 1 NL + 1 TE | 3 vé, daDat=10, conTrong=0, soTaiKhoan=3. |
| C02 | Đặt thêm 1 người D1 | 400, message chính xác “Lịch khởi hành không đủ chỗ trống.”; không thêm vé/hợp đồng. |
| C03 | D1 đầy, đặt D2: 2 người | D2 vẫn đặt được. Không cộng khách D1 vào D2. |
| C04 | Một tài khoản đặt nhiều vé cùng lịch | Mỗi vé vẫn một dòng; soTaiKhoan đếm distinct; daDat cộng tất cả người. |
| C05 | A chưa trả tiền hủy vé 4 chỗ, GET lại | DaHuy; D1 daDat=6, conTrong=4; list còn 2 vé. |
| C06 | Lặp C01, A có khoản đã xác nhận rồi hủy | ChoHoanTien; ngay GET sau đã trả 4 chỗ, không chờ xác nhận hoàn mới trả chỗ. |
| C07 | Hai request đồng thời tranh 1 chỗ cuối | Chỉ một 201; một 400; tổng giữ không vượt sức chứa. Phải chạy trên SQL Server thật. |
| C08 | SoCho=NULL, SoCho=0; sửa SoCho thấp hơn daDat | NULL dùng Slkhach; 0 không đặt được; giảm thấp hơn daDat trả 409. |
| C09 | Sửa Slkhach thấp hơn daDat của lịch SoCho=NULL | 409, không làm âm số chỗ còn trống do thay mặc định. |
| C10 | Tour không có lịch hoặc chỉ có lịch đã qua | “Chưa có lịch khởi hành”, nút đặt disabled; API từ chối đặt lịch đã qua. |
| C11 | Tổng người vượt conTrong, âm, lẻ, bằng 0 | UI không gửi booking; API từ chối đầu vào không hợp lệ. |
| H01 | Staff mở tour → lịch → vé | Có ngày, sức chứa/đã đặt/còn trống, tóm tắt “3 tài khoản · 10/10 chỗ”; không gộp vé. |
| H02 | Mở vé đã gắn MaKhachHang, tài khoản có nhiều hồ sơ | Trả đúng hồ sơ gắn vé, không lấy nhầm hồ sơ khác của tài khoản. |
| H03 | Mở vé không gắn MaKhachHang | Fallback theo MaUser; giấy tờ có loại/số/ngày cấp/ngày hết hạn/nơi cấp. |
| H04 | Vé không tồn tại; vé chưa có hồ sơ | Lần lượt 404; 200 với trường hồ sơ null và giấy tờ rỗng. |
| H05 | Khách gọi API danh sách khách/hồ sơ staff | 403; chưa đăng nhập: 401. |
| D01 | Khách chọn đề xuất, staff sửa/lưu LichTrinh khác đề xuất | DangThietKe; giá/lịch hiện tại phản ánh bản đã lưu. |
| D02 | Staff gửi duyệt, chưa có đồng ý từ khách | ChoKhachXacNhan; tour chưa duyệt; nút duyệt disabled, API duyệt và từ chối staff đều 409. |
| D03 | Khách mở /tu-thiet-ke/{id} | Thấy LichTrinh đã sửa và giá, không chỉ ba thẻ đề xuất; có hai nút phản hồi; chưa có nút đặt. |
| D04 | Khách yêu cầu chỉnh với lý do trống / có nội dung | Trống: 400; có nội dung: CanChinhSua, tour Nhap, nguonLyDo=KhachHang. |
| D05 | Staff sửa/lưu sau D04 | Lý do khách còn giữ. Gửi duyệt lại mới xóa cả lyDo/nguonLyDo thành null. |
| D06 | Khách đồng ý, staff duyệt | ChoDuyet → DaDuyet; lần duyệt đầu 200, gọi lại 409; sau duyệt mới có nút đặt. |
| D07 | Sau đồng ý, staff từ chối với lý do | CanChinhSua; lưu tiền tố Admin trong DB, JSON/UI chỉ hiện nội dung và nguồn Admin. |
| D08 | Lý do cũ trong DB không có tiền tố | Đọc nguồn Admin; không sửa thêm schema hay yêu cầu migration dữ liệu lý do. |
| D09 | Đổi giá/lịch bằng API Tour/LichTrinh khi chờ khách, chờ duyệt hoặc đã duyệt | Bị chặn; không thể né luồng đồng ý bằng màn hình Quản lý tour. |
| D10 | Khách khác đoán id yêu cầu/tour tự thiết kế | Không đọc được lịch riêng, không đồng ý/chỉnh sửa thay chủ yêu cầu. Catalog mặc định không lộ tour riêng. |
| D11 | Form sửa tour Chuan và TuThietKe | Nhãn đúng loại tour đang mở; không hiện “Tour mới thuộc loại Chuan” khi đang sửa tour tự thiết kế. |

## 5. Đã chạy và chưa chạy

- Backend `dotnet build TourDuLich.slnx --no-restore`: thành công, 0 warning, 0 error.
- Nhóm unit/controller/SQL-translation hiện tại: **92 passed, 0 failed, 0 skipped**. Không dùng kết quả này để khẳng định đã thử đồng thời trên SQL Server.
- Giao diện khách: **7 kiểm tra render/validation passed** (không lịch, lịch cũ/đầy, tổng người, ẩn đặt trước duyệt, lịch đã lưu, quyền phản hồi theo trạng thái). Đây là kiểm tra SSR/component, không phải bấm E2E trong trình duyệt.
- Hai frontend: build production thành công qua Vite API với chính cấu hình hiện có. `npm run build` trong môi trường agent gặp Access denied lúc esbuild đọc thư mục cha C:/Users/khait; không sửa package/config để né lỗi môi trường.
- SQL trực tiếp: chưa chạy được. Kết nối `localhost` thất bại; instance SQLEXPRESS có chạy nhưng kết nối Windows vào `.\SQLEXPRESS` báo **Failed to generate SSPI context**. Không đổi chuỗi kết nối, không thay xác thực và không ghi dữ liệu.
- HTTP/SQL acceptance: đã bổ sung nhưng **CHƯA CHẠY**: `DepartureCapacityFlowTests` (3 ca: hai nhánh hủy và concurrent last-seat), cập nhật `SelfDesignedTourFlowTests`. Cần DB test riêng để xác minh giao dịch/khóa và kết quả HTTP thực tế.
- Các chức năng bị loại khỏi prompt không được nghiệm thu lại toàn bộ trong vòng này. Không tuyên bố toàn hệ thống đã sạch lỗi.

### Chạy lại các kiểm tra không cần DB

PowerShell, thư mục backend:

```powershell
Set-Location 'C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend'
dotnet build TourDuLich.slnx --no-restore
dotnet test src/TourDuLich.IntegrationTests/TourDuLich.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~DepartureAvailabilityTests|FullyQualifiedName~DesignRevisionReasonTests|FullyQualifiedName~SelfDesignedTourControllerTests|FullyQualifiedName~TourScheduleManagementTests|FullyQualifiedName~YeuCauThietKeStateMachineTests|FullyQualifiedName~BookingPaymentControllerTests|FullyQualifiedName~TuThietKeTourPricingTests"
Set-Location 'C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client'
node --test tests/departure-ui.test.mjs
```

Trong mỗi frontend, build thông thường bằng `npm run build`. Lệnh đã xác minh được trong môi trường agent (đọc cùng vite.config.js, không sửa cấu hình):

```powershell
node --input-type=module -e "import config from './vite.config.js'; import {build} from 'vite'; await build({...config,configFile:false});"
```

### Chạy acceptance trên SQL thật — chỉ dùng database test riêng

Không chạy toàn bộ suite vào TourDuLich thật: fixture cũ có câu cleanup xóa dữ liệu theo tiền tố tài khoản/vé test. Hai lớp acceptance trong vòng này được chặn khởi động nếu không chỉ rõ database tên bắt đầu `TourDuLich_Test_`.

Chuẩn bị **database test riêng** được dựng theo các schema của bản C, có seed tài khoản test, điểm tham quan/sản phẩm và migration 016. Chọn đúng server đang dùng; ví dụ dưới chỉ dùng khi database test đó thực sự đã được tạo trên SQLEXPRESS:

```powershell
Set-Location 'C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend'
$env:TOURDULICH_TEST_CONNECTION='Server=.\SQLEXPRESS;Database=TourDuLich_Test_Departures;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test src/TourDuLich.IntegrationTests/TourDuLich.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~DepartureCapacityFlowTests|FullyQualifiedName~SelfDesignedTourFlowTests"
```

Nếu dùng Azure SQL, đặt kết nối DB test trong biến môi trường trên máy dev/CI, không đưa mật khẩu vào Git hoặc gửi vào hội thoại. Các thay đổi database do suite là dữ liệu kiểm thử, không phải triển khai migration vào database ứng dụng.

## 6. Danh sách file thay đổi

Các đường dẫn bên dưới thuộc bản C. Hai file patch đã có từ trước được giữ nguyên, không tính vào vòng sửa này.

- [backend/src/TourDuLich.API/Controllers/DatDichVuController.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Controllers/DatDichVuController.cs)
- [backend/src/TourDuLich.API/Controllers/LichKhoiHanhController.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Controllers/LichKhoiHanhController.cs)
- [backend/src/TourDuLich.API/Controllers/LichTrinhController.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Controllers/LichTrinhController.cs)
- [backend/src/TourDuLich.API/Controllers/TourController.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Controllers/TourController.cs)
- [backend/src/TourDuLich.API/Controllers/YeuCauThietKeController.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Controllers/YeuCauThietKeController.cs)
- [backend/src/TourDuLich.API/DTOs/DesignRequestView.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/DTOs/DesignRequestView.cs)
- [backend/src/TourDuLich.API/DTOs/LichKhoiHanhCreateDto.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/DTOs/LichKhoiHanhCreateDto.cs)
- [backend/src/TourDuLich.API/DTOs/LichKhoiHanhUpdateDto.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/DTOs/LichKhoiHanhUpdateDto.cs)
- [backend/src/TourDuLich.API/Services/DepartureAvailability.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.API/Services/DepartureAvailability.cs)
- [backend/src/TourDuLich.Application/Services/DesignRevisionReason.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.Application/Services/DesignRevisionReason.cs)
- [backend/src/TourDuLich.Application/Services/YeuCauThietKeStateMachine.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.Application/Services/YeuCauThietKeStateMachine.cs)
- [backend/src/TourDuLich.Infrastructure/Entities/LichKhoiHanh.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.Infrastructure/Entities/LichKhoiHanh.cs)
- [backend/src/TourDuLich.IntegrationTests/DepartureAvailabilityTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/DepartureAvailabilityTests.cs)
- [backend/src/TourDuLich.IntegrationTests/DepartureCapacityFlowTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/DepartureCapacityFlowTests.cs)
- [backend/src/TourDuLich.IntegrationTests/DesignRevisionReasonTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/DesignRevisionReasonTests.cs)
- [backend/src/TourDuLich.IntegrationTests/KeyQuerySet.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/KeyQuerySet.cs)
- [backend/src/TourDuLich.IntegrationTests/SelfDesignedTourControllerTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/SelfDesignedTourControllerTests.cs)
- [backend/src/TourDuLich.IntegrationTests/SelfDesignedTourFlowTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/SelfDesignedTourFlowTests.cs)
- [backend/src/TourDuLich.IntegrationTests/TourScheduleManagementTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/TourScheduleManagementTests.cs)
- [database/schema/016_LichKhoiHanh_SoCho.sql](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/database/schema/016_LichKhoiHanh_SoCho.sql)
- [frontend-admin/app/src/DepartureGuests.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/frontend-admin/app/src/DepartureGuests.jsx)
- [frontend-admin/app/src/DesignRequests.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/frontend-admin/app/src/DesignRequests.jsx)
- [frontend-admin/app/src/api.js](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/frontend-admin/app/src/api.js)
- [frontend-admin/app/src/pages.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/frontend-admin/app/src/pages.jsx)
- [wavv-main/client/src/App.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/App.jsx)
- [wavv-main/client/src/CurrentDesignSchedule.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/CurrentDesignSchedule.jsx)
- [wavv-main/client/src/DepartureBooking.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/DepartureBooking.jsx)
- [wavv-main/client/src/DesignRequests.jsx](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/DesignRequests.jsx)
- [wavv-main/client/src/api.js](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/api.js)
- [wavv-main/client/src/departureAvailability.mjs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/src/departureAvailability.mjs)
- [wavv-main/client/tests/departure-ui.test.mjs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/wavv-main/client/tests/departure-ui.test.mjs)
- [backend/src/TourDuLich.IntegrationTests/BookingPaymentControllerTests.cs](C:/Users/khait/Downloads/DoAn_TourDuLich/DoAn_TourDuLich/DoAn_TourDuLich/backend/src/TourDuLich.IntegrationTests/BookingPaymentControllerTests.cs)
