from pathlib import Path
import sys

import pandas as pd

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.data_loader import load_danh_gia_tour, load_hanh_vi, load_bookings_active, load_tours, load_wishlist  # noqa: E402
from app.evaluation import precision_recall_at_k, train_test_split_hanh_vi  # noqa: E402
from app.hybrid import recommend  # noqa: E402


def main() -> None:
    interactions = load_hanh_vi()
    tours = load_tours()
    ratings = load_danh_gia_tour()
    bookings = load_bookings_active()
    wishlist = load_wishlist()
    if interactions.empty or tours.empty:
        print("Không đủ dữ liệu để đánh giá. Hãy cấu hình AI_DB_CONNECTION và nạp dữ liệu.")
        return
    train, test = train_test_split_hanh_vi(interactions)
    train_pairs = set(zip(train["MaUser"].astype(str), train["MaTour"].astype(str)))
    ratings = ratings[ratings.apply(lambda row: (str(row.MaUser), str(row.MaTour)) in train_pairs, axis=1)]
    bookings = bookings[bookings.apply(lambda row: (str(row.MaUser), str(row.MaTour)) in train_pairs, axis=1)]
    wishlist = wishlist[wishlist.apply(lambda row: (str(row.MaUser), str(row.MaTour)) in train_pairs, axis=1)]
    empty = pd.DataFrame(columns=["MaUser", "MaTour"])
    print("Phương pháp\tPrecision@5\tRecall@5")
    configurations = [
        ("Content-based", "content", 1.0),
        ("Collaborative", "collaborative", 0.0),
        ("Hybrid (α=0.5)", "hybrid", 0.5),
        ("SVD", "svd", 0.5),
    ]
    for name, algorithm, alpha in configurations:
        def callback(user_id: str, available_tours: pd.DataFrame, k: int):
            return recommend(user_id, available_tours, train, ratings, bookings, wishlist,
                             empty, k, alpha, algorithm)

        precision, recall = precision_recall_at_k(callback, test, tours, 5)
        print(f"{name}\t{precision:.4f}\t\t{recall:.4f}")


if __name__ == "__main__":
    main()
