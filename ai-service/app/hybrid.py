import pandas as pd
from . import content_based, collaborative, matrix_factorization


def recommend(user_id: str, tours: pd.DataFrame, interactions: pd.DataFrame,
              ratings: pd.DataFrame, bookings: pd.DataFrame, wishlist: pd.DataFrame,
              rejected: pd.DataFrame | None = None, so_luong: int = 5,
              alpha: float = 0.5, algorithm: str = "hybrid") -> list[dict]:
    if tours.empty:
        return []
    algorithm = algorithm.lower()
    if algorithm not in {"content", "collaborative", "hybrid", "svd"}:
        raise ValueError("algorithm phải là content, collaborative, hybrid hoặc svd.")
    rejected_ids = set()
    if rejected is not None and not rejected.empty:
        rejected_ids = set(rejected.loc[rejected["MaUser"] == user_id, "MaTour"].astype(str)) if "MaTour" in rejected else set()
    positive = None
    collaborative_scores = None
    svd_scores = None
    if algorithm in {"content", "hybrid"}:
        positive = content_based.score_user(tours, interactions, ratings, user_id, wishlist, rejected_ids)
    if algorithm in {"collaborative", "hybrid"}:
        collaborative_scores = collaborative.score_user(interactions, user_id)
    if algorithm == "svd":
        svd_scores = matrix_factorization.score_user(interactions, user_id)
    candidates = tours.copy()
    booked = set(bookings.loc[bookings["MaUser"] == user_id, "MaTour"].astype(str)) if not bookings.empty else set()
    candidates = candidates[~candidates["MaTour"].astype(str).isin(booked | rejected_ids)]
    if candidates.empty:
        return []
    selected_scores = svd_scores if algorithm == "svd" else (
        positive if algorithm == "content" else collaborative_scores if algorithm == "collaborative" else None)
    if algorithm == "hybrid":
        has_signal = positive is not None or collaborative_scores is not None
    else:
        has_signal = selected_scores is not None

    if not has_signal:
        scores = {str(row.MaTour): 0.0 for row in candidates.itertuples()}
        if not ratings.empty:
            averages = ratings.groupby("MaTour")["SaoDanhGia"].mean().to_dict()
            scores = {key: float(averages.get(key, 0) / 5) for key in scores}
        if not bookings.empty:
            completed = bookings[bookings["TrangThai"].astype(str).str.strip() == "HoanThanh"]
            booking_counts = completed.groupby("MaTour").size().to_dict()
            max_count = max(booking_counts.values(), default=0)
            if max_count:
                scores = {key: 0.5 * value + 0.5 * booking_counts.get(key, 0) / max_count
                          for key, value in scores.items()}
        reason = "Tour phổ biến được nhiều khách hàng đánh giá tốt"
    else:
        scores = {}
        for tour_id in candidates["MaTour"].astype(str):
            if algorithm == "content":
                scores[tour_id] = (positive or {}).get(tour_id, 0.0)
            elif algorithm == "collaborative":
                scores[tour_id] = (collaborative_scores or {}).get(tour_id, 0.0)
            elif algorithm == "svd":
                scores[tour_id] = (svd_scores or {}).get(tour_id, 0.0)
            else:
                content_score = (positive or {}).get(tour_id, 0.0)
                cf_score = (collaborative_scores or {}).get(tour_id, 0.0)
                scores[tour_id] = alpha * content_score + (1 - alpha) * cf_score
        reason = {
            "content": "Phù hợp với sở thích bạn đã thể hiện qua các tour đã xem/thích",
            "collaborative": "Khách hàng có sở thích tương tự bạn cũng đã chọn tour này",
            "svd": "Điểm dự đoán từ mô hình SVD trên lịch sử tương tác của bạn",
            "hybrid": "Kết hợp sở thích cá nhân và hành vi của khách hàng tương tự",
        }[algorithm]
    return [{"maTour": key, "diemPhuHop": round(value, 4), "lyDo": reason}
            for key, value in sorted(scores.items(), key=lambda item: item[1], reverse=True)[:so_luong]]
