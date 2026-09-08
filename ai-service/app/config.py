import os
from dotenv import load_dotenv

load_dotenv()

AI_DB_CONNECTION = os.getenv("AI_DB_CONNECTION", "")
INTERNAL_API_KEY = os.getenv("INTERNAL_API_KEY", "")
# Recommendation inputs must be re-read within the agreed 7-minute SLA.
AI_REFRESH_INTERVAL_SECONDS = min(
    max(1, int(os.getenv("AI_REFRESH_INTERVAL_SECONDS", "300"))), 420
)
AI_CACHE_TTL_SECONDS = min(
    max(1, int(os.getenv("AI_CACHE_TTL_SECONDS", str(AI_REFRESH_INTERVAL_SECONDS)))),
    AI_REFRESH_INTERVAL_SECONDS,
)
