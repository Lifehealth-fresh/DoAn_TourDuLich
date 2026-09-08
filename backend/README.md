# TourDuLich backend

Backend ASP.NET Core gọi `ai-service` khi chạy `POST /api/AiGoiY/sinh-goi-y`.

Chạy song song:

```bash
dotnet run --project src/TourDuLich.API
cd ../ai-service
uvicorn app.main:app --reload --port 8000
```

Đặt `AiService:BaseUrl` về `http://localhost:8000/` và `AiService:ApiKey` giống
`INTERNAL_API_KEY` của Python. Python chỉ đọc Azure SQL; ASP.NET mới ghi bảng `AIGoiY`.
