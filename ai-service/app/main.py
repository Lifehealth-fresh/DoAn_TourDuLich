from fastapi import FastAPI, Header, HTTPException
from .config import AI_REFRESH_INTERVAL_SECONDS, INTERNAL_API_KEY
from .data_loader import (load_bookings_active, load_danh_gia_tour, load_hanh_vi,
                          check_database, get_latest_recommendation_age_seconds, load_rejected_requests,
                          load_tours, load_wishlist, validate_schema)
from .hybrid import recommend
from .schemas import Recommendation, RecommendationRequest

app = FastAPI(title="TourDuLich AI Recommendation Service")


@app.on_event("startup")
def validate_shared_schema() -> None:
    validate_schema()


@app.get("/health")
def health():
    try:
        check_database()
        age_seconds = get_latest_recommendation_age_seconds()
    except Exception as error:
        raise HTTPException(status_code=503, detail={
            "status": "degraded", "database": "unavailable", "message": str(error)
        }) from error
    return {"status": "ok", "database": "reachable",
            "refreshIntervalSeconds": AI_REFRESH_INTERVAL_SECONDS,
            "latestRecommendationAgeSeconds": age_seconds}


@app.post("/goi-y", response_model=list[Recommendation])
def get_recommendations(request: RecommendationRequest, x_internal_api_key: str | None = Header(default=None)):
    if not INTERNAL_API_KEY or x_internal_api_key != INTERNAL_API_KEY:
        raise HTTPException(status_code=401, detail="Invalid internal API key")
    return recommend(request.maUser, load_tours(), load_hanh_vi(), load_danh_gia_tour(),
                     load_bookings_active(), load_wishlist(), load_rejected_requests(),
                     request.soLuong, request.alpha, request.algorithm)
