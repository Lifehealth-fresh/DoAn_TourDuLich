# Media tour trên Cloudinary

Backend là thành phần duy nhất gọi Cloudinary. Frontend không nhận API secret.

## Cấu hình local

Đặt biến môi trường `CLOUDINARY_URL` cho tiến trình ASP.NET Core, hoặc dùng .NET User Secrets với khóa `Cloudinary:Url`. Không lưu URL chứa API secret trong `appsettings*.json` hay commit vào Git.

```powershell
dotnet user-secrets set "Cloudinary:Url" "cloudinary://<api_key>:<api_secret>@<cloud_name>" --project backend/src/TourDuLich.API/TourDuLich.API.csproj
```

Chạy schema theo thứ tự đến `012_AnhTour_Cloudinary.sql`. Migration này lưu `CloudPublicId` và `CloudResourceType`; hai trường này giúp backend xóa asset đúng loại trên Cloudinary khi Sale/Admin thay hoặc xóa media.

## API

- `POST /api/AnhTour/theo-tour/{maTour}/upload`: upload media mới.
- `PUT /api/AnhTour/{maAnhTour}/upload`: thay media hiện có.
- `DELETE /api/AnhTour/{maAnhTour}`: xóa metadata và dọn asset Cloudinary theo cơ chế best-effort có log lỗi.

Ba endpoint chỉ dành cho `Sale,Admin`. `GET /api/AnhTour/theo-tour/{maTour}` là public. Endpoint JSON URL cũ trả `410 Gone` để không còn lưu URL/đường dẫn tùy ý.

## Quy tắc kiểm tra

| Loại | Extension | MIME bắt buộc | File signature | Tối đa |
|---|---|---|---|---|
| Ảnh | JPG/JPEG, PNG, WebP | image/jpeg, image/png, image/webp | JPEG/PNG/RIFF-WEBP | 10 MB |
| Video | MP4, WebM | video/mp4, video/webm | `ftyp`/EBML | 100 MB |

Backend xác minh đồng thời extension, MIME và bytes đầu file; không tin riêng `Content-Type` do trình duyệt gửi. Asset được upload public và backend chỉ lưu HTTPS URL Cloudinary trả về.
