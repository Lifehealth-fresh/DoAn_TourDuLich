import pandas as pd
from app.evaluation import precision_recall_at_k, train_test_split_hanh_vi


def test_split_keeps_latest_interaction_for_test():
    interactions = pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T1", "ThoiGian": "2026-01-01"},
        {"MaUser": "U1", "MaTour": "T2", "ThoiGian": "2026-01-02"},
        {"MaUser": "U1", "MaTour": "T3", "ThoiGian": "2026-01-03"},
    ])
    train, test = train_test_split_hanh_vi(interactions, 1 / 3)
    assert set(test["MaTour"]) == {"T3"}
    assert set(train["MaTour"]) == {"T1", "T2"}


def test_precision_recall_are_in_zero_one_range():
    test_set = pd.DataFrame([
        {"MaUser": "U1", "MaTour": "T2"},
        {"MaUser": "U1", "MaTour": "T3"},
    ])
    tours = pd.DataFrame([{"MaTour": "T1"}, {"MaTour": "T2"}, {"MaTour": "T3"}])

    def recommend(user_id, available_tours, k):
        return [{"maTour": "T2"}, {"maTour": "T1"}]

    precision, recall = precision_recall_at_k(recommend, test_set, tours, 2)
    assert precision == 0.5
    assert recall == 0.5
