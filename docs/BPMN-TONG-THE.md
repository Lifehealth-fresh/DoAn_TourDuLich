# SƠ ĐỒ NGHIỆP VỤ TỔNG THỂ — HỆ THỐNG CÁ NHÂN HÓA DU LỊCH (BPMN 2.0)

> Nguồn chân lý: code trong `D:\DoAn\DoAn_TourDuLich` (21 controller, `DeXuatLichTrinhService`, `HanhViLogger`, schema 001-009). Ký hiệu trạng thái trong sơ đồ khớp code sau Prompt 2-4. File này render trực tiếp trên GitHub (mermaid).

---

## 1. Sơ đồ tổng quan end-to-end (6 swimlane)

```mermaid
flowchart TD
    subgraph KH[Khách hàng]
        K1[Duyệt Tour / Tìm kiếm / Xem chi tiết<br/>GET /api/Tour]
        K2[Yêu thích tour<br/>POST /api/DanhSachYeuThich]
        K3[Yêu cầu AI gợi ý<br/>nhập điểm đến / ngày / ngân sách / sở thích]
        K4{Xem gợi ý<br/>GET /api/AiGoiY/cua-toi}
        K5[Chọn tour có sẵn<br/>để đặt]
        K6[Từ chối gợi ý<br/>kèm lý do — KHÔNG bắt buộc tạo YC]
        K7[Tạo YeuCauThietKe<br/>POST /api/YeuCauThietKe<br/>DiemDen/SoNgay/SoKhach/NganSach/SoThich<br/>+ MaGoiYThamKhao? + LyDoTuChoiGoiY?]
        K8[Xem 3 phương án đề xuất<br/>GET .../de-xuat]
        K9[Chọn 1 phương án<br/>PUT .../chon-de-xuat/maDeXuat<br/>→ Tour TuThietKe Nhap]
        K10[Kiểm tra lịch trình<br/>đã chọn / đã được Sale sửa]
        K11[Đặt dịch vụ<br/>POST /api/DatDichVu<br/>MaTour + MaKhoiHanh + SL khách]
        K12[Thanh toán<br/>POST /api/ThanhToan<br/>ChuyenKhoan hoặc TienMat]
        K13[Chờ Sale xác nhận<br/>thanh toán + ký HĐ]
        K14[Tham gia tour]
        K15[Đánh giá tour / HDV / sản phẩm<br/>POST /api/DanhGia/...<br/>hiển thị ngay]
    end

    subgraph FE[Frontend Customer<br/>wavv-main/client]
        F1[Render danh mục + chi tiết<br/>+ ghi hành vi Xem/TimKiem<br/>POST /api/HanhViKhachHang]
        F2[Giữ trạng thái yêu thích / gợi ý]
        F3[Form YeuCauThietKe<br/>+ hiển thị đề xuất]
        F4[Trang booking + tổng hợp<br/>GET .../tong-hop<br/>hiển thị conLai / conLaiKhaDung]
    end

    subgraph BE[Backend ASP.NET Core<br/>TourDuLich.API]
        B1[Validate + HanhViLogger<br/>Xem/TimKiem/DatTour/...]
        B2[AIGoiY: đọc/ghi bảng AIGoiY]
        B3[DeXuatLichTrinhService<br/>sinh 3 PA từ DiemThamQuan + SanPhamDoiTac]
        B4[Tạo Tour TuThietKe + LichTrinh<br/>trong transaction]
        B5[DatDichVu: Serializable + UPDLOCK<br/>check Slkhach — chống bán vượt]
        B6[Tạo HopDong DuThao<br/>snapshot DieuKhoanCamKet + HoTen/SoGiayTo]
        B7[ThanhToan: ChoXacNhan<br/>check conLaiKhaDung<br/>không webhook NH]
        B8[Khóa Tour.GiaTour/DieuKhoan<br/>khi đã có HopDong DaKy]
        B9[Validate DanhGia: phải HoanThanh<br/>mới được đánh]
    end

    subgraph SALE[Sale / Admin]
        S1[Duyệt danh mục Tour/LKH/LT<br/>Quản lý DoiTac/SanPham/KM]
        S2[Sinh đề xuất<br/>POST .../sinh-de-xuat]
        S3[Sửa lịch trình tour Nhap<br/>PUT .../sua-lich-trinh<br/>tính lại GiaTour]
        S4[Từ chối tour ChoXacNhan<br/>PUT .../tu-choi-boi-sale<br/>kèm LyDo → CanChinhSua]
        S5[Duyệt booking<br/>PUT .../trang-thai<br/>ChoXacNhan→DaXacNhan→DaThanhToan→HoanThanh]
        S6[Ký hợp đồng<br/>PUT /api/HopDong/maHD/ky<br/>DuThao→DaKy]
        S7[Xác nhận thanh toán<br/>PUT /api/ThanhToan/maTt/xac-nhan<br/>đối soát sao kê / thu tiền mặt]
        S8[Từ chối thanh toán<br/>PUT .../tu-choi]
    end

    subgraph AI[AI Service Python<br/>FastAPI + SQL Server]
        A1[Đọc HanhViKhachHang<br/>DatDichVu / YeuThich / HoSo<br/>mỗi ≤ 7 phút]
        A2[Hybrid CB+CF + K-Means<br/>+ fallback cold start PhoBien]
        A3[UPSERT AIGoiY<br/>MaRecommodation + DiemPhuHop + LyDo]
        A4[/health + JobRunLog/]
    end

    subgraph BI[Power BI + Views]
        P1[Đọc Views 005_ViewsBI.sql<br/>+ bảng DatDichVu/ThanhToan/HopDong]
        P2[Dashboard: doanh thu<br/>lấp đầy / chuyển đổi gợi ý / cụm KH]
    end

    K1 --> F1 --> B1
    K2 --> B1
    K3 --> B2
    B2 <--> A1
    A1 --> A2 --> A3 --> B2
    K4 -->|Có gợi ý phù hợp| K5 --> K11
    K4 -->|Không phù hợp| K6
    K6 --> K7
    K3 --> K7
    K7 --> B3
    B3 --> S2 --> B3
    B3 --> K8 --> K9 --> B4 --> K10
    K10 --> S3 --> B4 --> K10
    S4 -.-> K10
    K10 --> K11 --> B5 --> B6
    B5 -.->|vượt Slkhach| K11
    B6 --> K12 --> B7
    B7 --> S7
    B7 --> S8 --> K12
    S7 --> S5
    S5 --> S6 --> B8 --> K13 --> K14 --> K15 --> B9 --> B1
    B1 -.-> A1
    B5 & B6 & B7 & B9 -.-> P1 --> P2
```

**Luồng đọc:**
1. Khách duyệt/tìm/yêu thích → mỗi hành động được `HanhViLogger` ghi `HanhViKhachHang` (không phụ thuộc FE).
2. Khách yêu cầu AI gợi ý → AI service (≤7 phút) đọc hành vi + booking + yêu thích → hybrid → `UPSERT AIGoiY`. Khách mới (cold start) nhận fallback `PhoBien`/`SoThichBanDau`.
3. Nếu gợi ý không phù hợp, khách có thể từ chối kèm lý do (không bắt buộc) rồi tạo `YeuCauThietKe` (có thể kèm `MaGoiYThamKhao` để AI học).
4. Sale sinh 3 phương án (`TietKiem/CanBang/CaoCap`) từ `DiemThamQuan` + `SanPhamDoiTac`; khách chọn 1 → tạo `Tour TuThietKe (Nhap)` + `LichTrinh`; Sale có thể sửa lịch trình (tính lại `GiaTour`) hoặc từ chối (`ChoXacNhan → CanChinhSua`).
5. Khách đặt (`POST /api/DatDichVu`) — backend khóa `Tour` bằng `Serializable + UPDLOCK/HOLDLOCK`, check `Slkhach` rồi mới tạo `DatDichVu ChoXacNhan` + `HopDong DuThao` snapshot `DieuKhoanCamKet`.
6. Thanh toán mock: khách tạo `ThanhToan ChoXacNhan` (`ChuyenKhoan` — chờ Sale đối soát sao kê, hoặc `TienMat` — thu tại quầy/khách sạn, có thể trả sau khi trải nghiệm lưu trú); Sale `xac-nhan → DaXacNhan` (chặn vượt `ThanhTien`). Sale duyệt booking `DaXacNhan → DaThanhToan` khi đủ tiền, ký `HopDong DuThao → DaKy` (sau đó khóa `Tour.GiaTour/DieuKhoan`).
7. Sau `HoanThanh`, khách đánh giá (tour/HDV/sản phẩm) — chỉ khi đã `HoanThanh` mới được, hiển thị ngay, vẫn ghi hành vi. Toàn bộ dữ liệu chảy vào Power BI qua views.

---

## 2. Subprocess: Tự thiết kế tour (chi tiết)

```mermaid
flowchart TD
    Y1[Khách POST /api/YeuCauThietKe<br/>Moi] --> Y2{Có MaGoiYThamKhao?}
    Y2 -->|Có| Y3[Validate gợi ý thuộc user<br/>+ log TuChoiGoiY nếu có LyDo]
    Y2 -->|Không| Y4[Lưu Moi]
    Y3 --> Y4
    Y4 --> Y5[Sale POST .../sinh-de-xuat<br/>DeXuatLichTrinhService<br/>3 PA + TongTienDuKien]
    Y5 --> Y6[Khách GET .../de-xuat<br/>xem 3 PA]
    Y6 --> Y7[Khách PUT .../chon-de-xuat<br/>→ Tour TuThietKe Nhap<br/>+ LichTrinh + YeuCau DangThietKe]
    Y7 --> Y8{Sale cần chỉnh?}
    Y8 -->|Có| Y9[Sale PUT .../sua-lich-trinh<br/>khi Tour=Nhap<br/>tính lại GiaTour<br/>→ DangThietKe]
    Y8 -->|Không| Y10[Khách/Sale đưa Tour Nhap → ChoXacNhan<br/>để Sale duyệt]
    Y9 --> Y10
    Y10 --> Y11{Sale duyệt?}
    Y11 -->|Từ chối| Y12[PUT .../tu-choi-boi-sale<br/>Tour ChoXacNhan → Nhap<br/>YeuCau CanChinhSua + LyDo] --> Y9
    Y11 -->|Duyệt| Y13[Tour sẵn sàng để Khách<br/>POST /api/DatDichVu]
```

---

## 3. Subprocess: Thanh toán (rút gọn — chi tiết xem docs/QUY-TRINH-THANH-TOAN.md)

```mermaid
flowchart TD
    T1[Khách POST /api/ThanhToan<br/>ChoXacNhan<br/>DatCoc/ConLai/Du × ChuyenKhoan/TienMat] --> T2{conLaiKhaDung đủ?}
    T2 -->|Không| T3[400 Vượt phần còn lại]
    T2 -->|Có| T4[Sale PUT .../xac-nhan<br/>đối soát sao kê / thu tiền mặt]
    T4 --> T5{Đủ ThanhTien?}
    T5 -->|Chưa| T6[Chờ đợt tiếp — vòng lại T1]
    T5 -->|Đủ| T7[Sale PUT /api/DatDichVu/.../trang-thai<br/>DaXacNhan → DaThanhToan]
    T4 -.->|Thiếu/sai| T8[Sale PUT .../tu-choi<br/>→ khách tạo dòng mới] --> T1
```

---

## 4. Chú giải trạng thái (khớp code)

- **YeuCauThietKe:** `Moi → Huy` (khách, chỉ khi `Moi`), `Moi → DangThietKe` (chọn PA), `CanChinhSua ↔ DangThietKe` (Sale từ chối / sửa).
- **Tour TuThietKe:** `Nhap → ChoXacNhan → Nhap` (từ chối) hoặc `→` sẵn sàng đặt; khóa sửa `GiaTour/DieuKhoan` khi đã có `HopDong DaKy`.
- **DatDichVu:** `ChoXacNhan → DaXacNhan → DaThanhToan → HoanThanh`, nhánh `→ DaHuy` từ `ChoXacNhan/DaXacNhan` (kèm `TyLePhatHuy/SoTienPhatHuy` theo số ngày còn lại).
- **HopDong:** `DuThao → DaKy` (Sale ký), snapshot `DieuKhoanCamKet/HoTen/SoGiayTo`.
- **ThanhToan (sau Prompt 4):** `ChoXacNhan → DaXacNhan | TuChoi` (Sale duyệt), dữ liệu cũ `ThanhCong` tính như `DaXacNhan`; `tong-hop` trả `daThanhToan/dangCho/conLai/conLaiKhaDung`.

---

## 5. Tệp BPMN chuẩn để import Camunda / Visual Paradigm

Đã xuất kèm file `docs/BPMN-TONG-THE.bpmn` (BPMN 2.0 XML) — mở bằng Camunda Modeler hoặc https://demo.bpmn.io. Nếu cần bản `.png` để dán báo cáo, mở file `.bpmn` trên demo.bpmn.io và Export PNG.
