from pydantic import BaseModel, Field


class RecommendationRequest(BaseModel):
    maUser: str = Field(min_length=1)
    soLuong: int = Field(default=5, ge=1, le=50)
    alpha: float = Field(default=0.5, ge=0, le=1)
    algorithm: str = "hybrid"


class Recommendation(BaseModel):
    maTour: str
    diemPhuHop: float
    lyDo: str
