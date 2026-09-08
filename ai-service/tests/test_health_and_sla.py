import pytest
from fastapi import HTTPException

from app import main
from app.config import AI_CACHE_TTL_SECONDS, AI_REFRESH_INTERVAL_SECONDS


def test_refresh_configuration_never_exceeds_seven_minute_sla():
    assert 1 <= AI_REFRESH_INTERVAL_SECONDS <= 420
    assert 1 <= AI_CACHE_TTL_SECONDS <= AI_REFRESH_INTERVAL_SECONDS


def test_health_returns_503_when_database_is_unreachable(monkeypatch):
    monkeypatch.setattr(main, "check_database", lambda: (_ for _ in ()).throw(RuntimeError("db down")))
    with pytest.raises(HTTPException) as error:
        main.health()
    assert error.value.status_code == 503


def test_health_reports_database_when_reachable(monkeypatch):
    monkeypatch.setattr(main, "check_database", lambda: None)
    assert main.health()["database"] == "reachable"
