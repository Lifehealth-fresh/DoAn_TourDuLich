import numpy as np
import pandas as pd
from sklearn.metrics.pairwise import cosine_similarity


def score_user(interactions: pd.DataFrame, user_id: str) -> dict[str, float] | None:
    if interactions.empty or "trong_so" not in interactions:
        return None
    frame = interactions.copy()
    matrix = frame.pivot_table(index="MaUser", columns="MaTour", values="trong_so", aggfunc="sum", fill_value=0)
    if user_id not in matrix.index or len(matrix.columns) < 2:
        return None
    item_similarity = cosine_similarity(matrix.T)
    items = list(matrix.columns)
    user_values = matrix.loc[user_id].to_numpy(dtype=float)
    if not np.any(user_values > 0):
        return None
    raw = item_similarity @ user_values
    raw = np.maximum(raw, 0)
    max_value = raw.max()
    if max_value <= 0:
        return None
    return {str(item): float(value / max_value) for item, value in zip(items, raw)}
