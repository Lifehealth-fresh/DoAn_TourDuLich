import numpy as np
import pandas as pd
from sklearn.decomposition import TruncatedSVD


def score_user(
    interactions: pd.DataFrame,
    user_id: str,
    n_components: int = 10,
) -> dict[str, float] | None:
    if interactions.empty or "trong_so" not in interactions:
        return None

    matrix = interactions.pivot_table(
        index="MaUser", columns="MaTour", values="trong_so", aggfunc="sum", fill_value=0
    )
    if user_id not in matrix.index or matrix.shape[0] < 2 or matrix.shape[1] < 2:
        return None

    # TruncatedSVD needs fewer components than min(users, items); cap the requested
    # value so a normal 4x4 demo dataset can still use the default n_components=10.
    max_components = min(matrix.shape[0], matrix.shape[1]) - 1
    components = min(n_components, max_components)
    if components < 1:
        return None

    try:
        svd = TruncatedSVD(n_components=components, random_state=42)
        user_factors = svd.fit_transform(matrix)
        item_factors = svd.components_.T
    except (ValueError, np.linalg.LinAlgError):
        return None

    user_index = matrix.index.get_loc(user_id)
    predictions = user_factors[user_index] @ item_factors.T
    minimum = float(predictions.min())
    maximum = float(predictions.max())
    if maximum == minimum:
        normalized = np.zeros_like(predictions, dtype=float)
    else:
        normalized = (predictions - minimum) / (maximum - minimum)
    return {
        str(item): float(score)
        for item, score in zip(matrix.columns, normalized)
    }
