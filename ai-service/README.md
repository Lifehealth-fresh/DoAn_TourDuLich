# TourDuLich AI Recommendation Service

FastAPI service tính gợi ý Hybrid (content-based + item-based collaborative filtering).
Service chỉ đọc SQL Server qua `AI_DB_CONNECTION`, không ghi dữ liệu. ASP.NET Core là
nguồn duy nhất ghi bảng `AIGoiY`.

ASP.NET Core có worker làm mới `AIGoiY` mỗi 300 giây mặc định
(`AiService:RefreshIntervalSeconds`, bị giới hạn tối đa 420 giây). Python cũng giới
hạn cache input bằng `AI_CACHE_TTL_SECONDS`/`AI_REFRESH_INTERVAL_SECONDS` không quá
420 giây. Nhờ đó dữ liệu hành vi mới được phản ánh trong SLA tối đa 7 phút.

Lúc khởi động, service kiểm tra hợp đồng schema SQL và báo rõ bảng/cột thiếu nếu schema
không còn tương thích. `/health` trả tuổi của gợi ý mới nhất. Trạng thái mỗi lần worker
ASP.NET chạy được lưu ở `JobRunLog`, Admin xem qua `GET /api/ai-jobs/gan-nhat`.

## Chạy local

```bash
cd ai-service
python -m venv .venv
pip install -r requirements.txt
copy .env.example .env
uvicorn app.main:app --reload --port 8000
```

`INTERNAL_API_KEY` phải giống cấu hình `AiService:ApiKey` của ASP.NET. Endpoint `/health`
không cần key; `/goi-y` bắt buộc header `X-Internal-Api-Key`.

`POST /goi-y` hỗ trợ `algorithm`: `content` (Content-based), `collaborative`
(Item-based CF), `hybrid` (mặc định, kết hợp hai phương pháp) và `svd`
(Matrix Factorization dùng `TruncatedSVD`). Nếu không truyền field này, service vẫn chạy
Hybrid như trước.

Đánh giá offline chạy bằng:

```bash
python scripts/run_evaluation.py
```

Script in bảng Precision@5/Recall@5 để so sánh bốn cấu hình trên.

## Test

```bash
pytest
```
