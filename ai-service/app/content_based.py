import numpy as np
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.preprocessing import MinMaxScaler, OneHotEncoder
from sklearn.metrics.pairwise import cosine_similarity


def _tour_matrix(tours: pd.DataFrame):
    if tours.empty:
        return np.empty((0, 0))
    text = (tours.get("TenTour", "").fillna("") + " " + tours.get("Mota", "").fillna(""))
    vectorizer = TfidfVectorizer(token_pattern=r"(?u)\b\w+\b")
    try:
        text_matrix = vectorizer.fit_transform(text).toarray()
    except ValueError:
        text_matrix = np.zeros((len(tours), 0))
    categorical = tours.reindex(columns=["LoaiTour", "MaKhuVuc"], fill_value="").fillna("")
    categorical["MaKhuVuc"] = categorical["MaKhuVuc"].astype(str)
    cat_matrix = OneHotEncoder(handle_unknown="ignore", sparse_output=False).fit_transform(categorical)
    numeric = tours.reindex(columns=["GiaTour", "ThoiGian"], fill_value=0).fillna(0)
    numeric_matrix = MinMaxScaler().fit_transform(numeric)
    return np.hstack([text_matrix, cat_matrix, numeric_matrix])


def score_user(tours: pd.DataFrame, interactions: pd.DataFrame, ratings: pd.DataFrame,
               user_id: str, wishlist: pd.DataFrame | None = None,
               rejected: set[str] | None = None) -> dict[str, float] | None:
    if tours.empty:
        return None
    matrix = _tour_matrix(tours)
    positive: dict[str, float] = {}
    if not interactions.empty:
        for _, row in interactions[interactions["MaUser"] == user_id].iterrows():
            weight = float(row.get("trong_so", 0))
            if weight > 0:
                positive[str(row["MaTour"])] = max(positive.get(str(row["MaTour"]), 0), weight)
    if wishlist is not None and not wishlist.empty:
        for tour_id in wishlist.loc[wishlist["MaUser"] == user_id, "MaTour"].astype(str):
            positive[tour_id] = max(positive.get(tour_id, 0), 3)
    if not ratings.empty:
        for _, row in ratings[ratings["MaUser"] == user_id].iterrows():
            rating = float(row.get("SaoDanhGia", 0))
            if rating >= 4:
                positive[str(row["MaTour"])] = max(positive.get(str(row["MaTour"]), 0), rating)
            elif rating <= 2:
                positive.pop(str(row["MaTour"]), None)
    rejected = rejected or set()
    positive = {key: value for key, value in positive.items() if key not in rejected}
    indices = {str(value): index for index, value in enumerate(tours["MaTour"].astype(str))}
    used = [(indices[key], weight) for key, weight in positive.items() if key in indices]
    if not used:
        return None
    profile = sum(matrix[index] * weight for index, weight in used) / sum(weight for _, weight in used)
    scores = cosine_similarity(matrix, profile.reshape(1, -1)).ravel()
    scores = np.clip((scores + 1) / 2, 0, 1)
    return dict(zip(tours["MaTour"].astype(str), scores.tolist()))
