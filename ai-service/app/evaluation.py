from collections.abc import Callable
import math
import pandas as pd


def train_test_split_hanh_vi(
    interactions: pd.DataFrame, test_ratio: float = 0.2
) -> tuple[pd.DataFrame, pd.DataFrame]:
    if not 0 < test_ratio < 1:
        raise ValueError("test_ratio phải nằm trong khoảng (0, 1).")
    if interactions.empty:
        return interactions.copy(), interactions.copy()
    frame = interactions.copy()
    timestamps = frame["ThoiGian"] if "ThoiGian" in frame else pd.Series(index=frame.index, dtype="datetime64[ns]")
    frame["__time"] = pd.to_datetime(timestamps, errors="coerce")
    frame["__time"] = frame["__time"].fillna(pd.Timestamp.min)
    train_indexes: list[int] = []
    test_indexes: list[int] = []
    for _, group in frame.groupby("MaUser", sort=False):
        ordered = group.sort_values("__time")
        test_count = max(1, math.ceil(len(ordered) * test_ratio)) if len(ordered) > 1 else 0
        test_indexes.extend(ordered.tail(test_count).index.tolist())
        train_indexes.extend(ordered.head(len(ordered) - test_count).index.tolist())
    train = frame.loc[train_indexes].drop(columns="__time").sort_index()
    test = frame.loc[test_indexes].drop(columns="__time").sort_index()
    return train, test


def precision_recall_at_k(
    recommend_fn: Callable[[str, pd.DataFrame, pd.DataFrame, int], list[dict]],
    test_set: pd.DataFrame,
    tours: pd.DataFrame,
    k: int = 5,
) -> tuple[float, float]:
    if k <= 0:
        raise ValueError("k phải lớn hơn 0.")
    if test_set.empty:
        return 0.0, 0.0
    # The callback receives the full training frame through its closure.
    precisions: list[float] = []
    recalls: list[float] = []
    for user_id, group in test_set.groupby("MaUser"):
        expected = set(group["MaTour"].astype(str))
        recommendations = recommend_fn(str(user_id), tours, k)
        recommended = {str(item["maTour"]) for item in recommendations[:k]}
        hits = len(expected & recommended)
        precisions.append(hits / k)
        recalls.append(hits / len(expected) if expected else 0.0)
    return sum(precisions) / len(precisions), sum(recalls) / len(recalls)
