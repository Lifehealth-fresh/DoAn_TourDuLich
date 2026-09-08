import pandas as pd
from app.content_based import score_user


def test_different_user_preferences_produce_different_scores():
    tours = pd.DataFrame([
        {"MaTour": "T1", "TenTour": "Biển", "Mota": "Nghỉ dưỡng biển", "LoaiTour": "NghiDuong", "MaKhuVuc": "V1", "GiaTour": 10, "ThoiGian": 2},
        {"MaTour": "T2", "TenTour": "Núi", "Mota": "Khám phá núi", "LoaiTour": "KhamPha", "MaKhuVuc": "V2", "GiaTour": 20, "ThoiGian": 3},
        {"MaTour": "T3", "TenTour": "Biển đảo", "Mota": "Biển và nghỉ dưỡng", "LoaiTour": "NghiDuong", "MaKhuVuc": "V1", "GiaTour": 12, "ThoiGian": 2},
    ])
    events = pd.DataFrame([{"MaUser": "U1", "MaTour": "T1", "trong_so": 5}, {"MaUser": "U2", "MaTour": "T2", "trong_so": 5}])
    ratings = pd.DataFrame(columns=["MaUser", "MaTour", "SaoDanhGia"])
    first = score_user(tours, events, ratings, "U1")
    second = score_user(tours, events, ratings, "U2")
    assert first["T3"] > first["T2"]
    assert second["T2"] > second["T3"]
