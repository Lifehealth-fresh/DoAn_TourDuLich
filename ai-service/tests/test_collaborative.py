import pandas as pd
from app.collaborative import score_user


def test_similar_tours_receive_related_scores():
    events = pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T1", "trong_so": 5}, {"MaUser": "U1", "MaTour": "T2", "trong_so": 5},
        {"MaUser": "U2", "MaTour": "T1", "trong_so": 5}, {"MaUser": "U2", "MaTour": "T2", "trong_so": 5},
        {"MaUser": "U3", "MaTour": "T3", "trong_so": 5},
    ])
    scores = score_user(events, "U1")
    assert scores is not None
    assert scores["T2"] > 0.8
