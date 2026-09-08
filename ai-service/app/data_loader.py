import time
import pandas as pd
import pyodbc

from .config import AI_DB_CONNECTION, AI_CACHE_TTL_SECONDS

_cache: dict[str, tuple[float, pd.DataFrame]] = {}

# One explicit schema contract prevents silent empty recommendations after a rename.
REQUIRED_COLUMNS: dict[str, set[str]] = {
    "Tour": {"MaTour", "TenTour", "GiaTour", "TrangThai"},
    "HanhViKhachHang": {"MaHanhDong", "MaUser", "MaTour", "HanhDong", "ThoiGian"},
    "DatDichVu": {"MaUser", "MaTour", "TrangThai"},
    "DanhSachYeuThich": {"MaUser", "MaTour", "NgayThem"},
    "DanhGiaTour": {"MaUser", "MaTour", "SaoDanhGia"},
    "AIGoiY": {"MaRecommodation", "MaUser", "MaTour", "NgayGoiY"},
    "YeuCauThietKe": {"MaUser", "MaGoiYThamKhao", "LyDoTuChoiGoiY"},
}


def _read_cached(name: str, query: str) -> pd.DataFrame:
    now = time.time()
    cached = _cache.get(name)
    if cached and now - cached[0] < AI_CACHE_TTL_SECONDS:
        return cached[1].copy()
    if not AI_DB_CONNECTION:
        return pd.DataFrame()
    with pyodbc.connect(AI_DB_CONNECTION) as connection:
        cursor = connection.cursor()
        cursor.execute(query)
        columns = [column[0] for column in cursor.description]
        frame = pd.DataFrame.from_records(cursor.fetchall(), columns=columns)
    _cache[name] = (now, frame.copy())
    return frame


def clear_cache() -> None:
    _cache.clear()


def check_database() -> None:
    """Raise a clear error when SQL Server is unavailable for health checks."""
    if not AI_DB_CONNECTION:
        raise RuntimeError("AI_DB_CONNECTION is not configured")
    with pyodbc.connect(AI_DB_CONNECTION, timeout=5) as connection:
        connection.execute("SELECT 1")


def validate_schema() -> None:
    """Fail fast with the exact missing table/column when the shared contract changes."""
    if not AI_DB_CONNECTION:
        raise RuntimeError("AI_DB_CONNECTION is not configured")
    with pyodbc.connect(AI_DB_CONNECTION, timeout=5) as connection:
        rows = connection.execute(
            "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo'"
        ).fetchall()
    available: dict[str, set[str]] = {}
    for table, column in rows:
        available.setdefault(str(table), set()).add(str(column))
    missing = [f"{table}.{column}" for table, columns in REQUIRED_COLUMNS.items()
               for column in columns if column not in available.get(table, set())]
    if missing:
        raise RuntimeError("AI schema contract is missing: " + ", ".join(sorted(missing)))


def get_latest_recommendation_age_seconds() -> float | None:
    if not AI_DB_CONNECTION:
        return None
    with pyodbc.connect(AI_DB_CONNECTION, timeout=5) as connection:
        value = connection.execute(
            "SELECT DATEDIFF_BIG(second, MAX(NgayGoiY), SYSUTCDATETIME()) FROM dbo.AIGoiY"
        ).fetchval()
    return None if value is None else max(0, float(value))


def load_tours() -> pd.DataFrame:
    return _read_cached("tours", """
        SELECT t.MaTour, t.TenTour, t.Mota, t.GiaTour, t.ThoiGian,
               t.LoaiTour, STRING_AGG(CONVERT(nvarchar(20), kv.MaKhuVuc), ',') AS MaKhuVuc
        FROM dbo.Tour t
        LEFT JOIN dbo.LichTrinh lt ON lt.MaTour = t.MaTour
        LEFT JOIN dbo.DiemThamQuan dtq ON dtq.MaDthamQuan = lt.MaDthamQuan
        LEFT JOIN dbo.KhuVuc kv ON kv.MaKhuVuc = dtq.MaKhuVuc
        WHERE t.TrangThai = N'HoatDong'
        GROUP BY t.MaTour, t.TenTour, t.Mota, t.GiaTour, t.ThoiGian, t.LoaiTour
    """)


def load_hanh_vi() -> pd.DataFrame:
    frame = _read_cached("hanh_vi", """
        SELECT MaHanhDong, MaUser, MaTour, HanhDong, ThoiGian
        FROM dbo.HanhViKhachHang
    """)
    weights = {
        "Xem": 1, "TimKiem": 1, "XemLichTrinh": 2, "ThemYeuThich": 3,
        "DatTour": 5, "ThanhToan": 5, "HoanThanh": 5, "DanhGiaTour": 4,
        "DanhGiaHdv": 1, "DanhGiaSanPham": 1, "TuChoiGoiY": -3,
    }
    if not frame.empty:
        frame["trong_so"] = frame["HanhDong"].map(weights).fillna(0)
    return frame


def load_danh_gia_tour() -> pd.DataFrame:
    return _read_cached("danh_gia_tour", """
        SELECT MaUser, MaTour, SaoDanhGia
        FROM dbo.DanhGiaTour
    """)


def load_bookings_active() -> pd.DataFrame:
    return _read_cached("bookings_active", """
        SELECT MaUser, MaTour, TrangThai
        FROM dbo.DatDichVu
        WHERE TrangThai <> N'Huy' AND TrangThai <> N'DaHuy'
    """)


def load_wishlist() -> pd.DataFrame:
    return _read_cached("wishlist", """
        SELECT MaUser, MaTour, NgayThem
        FROM dbo.DanhSachYeuThich
    """)


def load_rejected_requests() -> pd.DataFrame:
    return _read_cached("rejected_requests", """
        SELECT yc.MaUser, goiy.MaTour, yc.LyDoTuChoiGoiY
        FROM dbo.YeuCauThietKe yc
        INNER JOIN dbo.AIGoiY goiy ON goiy.MaRecommodation = yc.MaGoiYThamKhao
        WHERE yc.LyDoTuChoiGoiY IS NOT NULL
          AND LTRIM(RTRIM(yc.LyDoTuChoiGoiY)) <> N''
    """)
