import pandas as pd
from app.hybrid import recommend


def test_cold_start_uses_popularity_fallback():
    tours = pd.DataFrame([
        {"MaTour": "T1", "TenTour": "A", "Mota": "", "LoaiTour": "A", "MaKhuVuc": "V1", "GiaTour": 1, "ThoiGian": 1},
        {"MaTour": "T2", "TenTour": "B", "Mota": "", "LoaiTour": "B", "MaKhuVuc": "V2", "GiaTour": 1, "ThoiGian": 1},
    ])
    ratings = pd.DataFrame([{"MaUser": "U1", "MaTour": "T1", "SaoDanhGia": 5}])
    empty = pd.DataFrame(columns=["MaUser", "MaTour"])
    result = recommend("NEW", tours, empty, ratings, empty, empty, empty)
    assert result[0]["maTour"] == "T1"


def test_svd_algorithm_returns_recommendations():
    tours = pd.DataFrame([
        {"MaTour": f"T{i}", "TenTour": f"Tour {i}", "Mota": "du lich", "LoaiTour": "A", "MaKhuVuc": "V1", "GiaTour": i, "ThoiGian": 2}
        for i in range(1, 5)
    ])
    interactions = pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T1", "trong_so": 5}, {"MaUser": "U1", "MaTour": "T2", "trong_so": 4},
        {"MaUser": "U2", "MaTour": "T1", "trong_so": 5}, {"MaUser": "U2", "MaTour": "T2", "trong_so": 4},
        {"MaUser": "U3", "MaTour": "T3", "trong_so": 5}, {"MaUser": "U3", "MaTour": "T4", "trong_so": 4},
        {"MaUser": "U4", "MaTour": "T3", "trong_so": 5}, {"MaUser": "U4", "MaTour": "T4", "trong_so": 4},
    ])
    empty = pd.DataFrame(columns=["MaUser", "MaTour"])
    result = recommend("U1", tours, interactions, empty, empty, empty, empty, algorithm="svd")
    assert result
    assert all("maTour" in item and "diemPhuHop" in item for item in result)
