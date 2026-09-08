import pandas as pd
from app.matrix_factorization import score_user


def _interactions():
    return pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T1", "trong_so": 5},
        {"MaUser": "U1", "MaTour": "T2", "trong_so": 4},
        {"MaUser": "U2", "MaTour": "T1", "trong_so": 5},
        {"MaUser": "U2", "MaTour": "T2", "trong_so": 4},
        {"MaUser": "U3", "MaTour": "T3", "trong_so": 5},
        {"MaUser": "U3", "MaTour": "T4", "trong_so": 4},
        {"MaUser": "U4", "MaTour": "T3", "trong_so": 5},
        {"MaUser": "U4", "MaTour": "T4", "trong_so": 4},
    ])


def test_svd_scores_user_pattern_without_error():
    scores = score_user(_interactions(), "U1")
    assert scores is not None
    assert set(scores) == {"T1", "T2", "T3", "T4"}
    assert all(0 <= value <= 1 for value in scores.values())
    assert scores["T1"] != scores["T3"]


def test_svd_cold_start_returns_none():
    assert score_user(_interactions(), "NEW") is None


def test_svd_too_small_returns_none():
    small = pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T1", "trong_so": 1},
        {"MaUser": "U1", "MaTour": "T2", "trong_so": 1},
    ])
    assert score_user(small, "U1") is None
