\# Hệ thống quản lý và cá nhân hóa tour du lịch



\## Giới thiệu



Hệ thống hỗ trợ khách hàng tìm kiếm, đặt tour, thanh toán mô phỏng,

nhận gợi ý cá nhân hóa và gửi yêu cầu tự thiết kế lịch trình.



\## Thành phần



\- Customer frontend: React, Vite

\- Admin/Sale frontend: React, Vite

\- Backend: ASP.NET Core 10

\- AI Service: FastAPI, scikit-learn

\- Database: SQL Server



\## Cấu trúc



\- `wavv-main/client`: website khách hàng

\- `frontend-admin/app`: website Admin/Sale

\- `backend`: ASP.NET Core API

\- `ai-service`: dịch vụ gợi ý AI

\- `database`: schema và migration

\- `docs`: tài liệu nghiệp vụ và kiểm thử



\## Cách chạy



1\. Khởi tạo database `TourDuLich`.

2\. Chạy AI Service tại cổng 8000.

3\. Chạy backend tại cổng 5265/7290.

4\. Chạy customer frontend tại cổng 5173.

5\. Chạy Admin frontend tại cổng 5174.



Xem hướng dẫn chi tiết trong README của từng thành phần.



\## Cấu hình



Sao chép các tệp `.env.example` và

`appsettings.Development.json.example` để tạo cấu hình local.

Không commit mật khẩu, API key hoặc connection string thật.



\## Mục tiêu review



Repository này được cung cấp để review:



\- Kiến trúc hệ thống

\- Lỗi tích hợp frontend và backend

\- Bảo mật và phân quyền

\- Hiệu năng

\- Chức năng gợi ý AI

\- Chức năng tự thiết kế lịch trình

\- Khả năng triển khai production