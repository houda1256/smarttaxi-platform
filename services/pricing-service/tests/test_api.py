import sys
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from fastapi.testclient import TestClient

from src.main import _state, app

client = TestClient(app)


def _sample_zone() -> str:
    return "tunis_centre"  # zone_id lisible (voir src/data_synthetic.py)


def test_health_ok():
    resp = client.get("/health")
    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert "model_version" in body


def test_predict_returns_full_contract():
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "available_drivers": 5,
    }
    resp = client.post("/predict", json=payload)
    assert resp.status_code == 200
    body = resp.json()

    for field in [
        "zone_id",
        "predicted_demand",
        "recommended_multiplier",
        "confidence",
        "model_version",
        "valid_until",
    ]:
        assert field in body


def test_multiplier_is_within_bounds():
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "available_drivers": 1,
    }
    resp = client.post("/predict", json=payload)
    body = resp.json()
    assert 0.8 <= body["recommended_multiplier"] <= 3.0


def test_low_history_zone_still_returns_a_value():
    # zone qui n'existe pas dans l'historique synthétique -> ne doit pas planter
    payload = {
        "zone_id": "zzzzzz",
        "timestamp": datetime.now(timezone.utc).isoformat(),
    }
    resp = client.post("/predict", json=payload)
    assert resp.status_code == 200
    assert resp.json()["predicted_demand"] >= 0


def test_invalid_payload_returns_422():
    resp = client.post("/predict", json={"zone_id": "abcdef"})  # timestamp manquant
    assert resp.status_code == 422


def test_naive_timestamp_rejected():
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": "2026-09-08T18:00:00",
    }
    resp = client.post("/predict", json=payload)
    assert resp.status_code == 422


def test_predict_fallback_when_xgb_unavailable():
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "available_drivers": 5,
    }
    with patch.object(_state["xgb"], "is_fitted", return_value=False):
        resp = client.post("/predict", json=payload)
    assert resp.status_code == 200
    body = resp.json()
    assert body["is_fallback"] is True
    assert body["confidence"] <= 0.5


def test_predict_with_weather_and_event_context():
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "available_drivers": 10,
        "weather": "rain",
        "is_event_nearby": True,
    }
    resp = client.post("/predict", json=payload)
    assert resp.status_code == 200
    body = resp.json()
    assert body["predicted_demand"] >= 0
    assert "météo=rain" in body["explanation"]


def test_weather_increases_predicted_demand():
    base_payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "available_drivers": 10,
    }
    base = client.post("/predict", json=base_payload).json()["predicted_demand"]
    with_weather = client.post(
        "/predict",
        json={**base_payload, "weather": "rain"},
    ).json()["predicted_demand"]
    assert with_weather > base


def test_peak_hour_demand_higher_than_off_peak():
    """Checklist officielle (Guide §11) : 'Heure de pointe vs heure normale'.

    Le générateur synthétique injecte deux pics (~8h, ~18h) — la demande
    prédite doit refléter ce profil, pas être plate sur 24h. Même zone,
    même jour, seule l'heure change.
    """
    zone = _sample_zone()
    peak = client.post(
        "/predict",
        json={"zone_id": zone, "timestamp": "2026-09-08T18:00:00+00:00"},
    ).json()
    off_peak = client.post(
        "/predict",
        json={"zone_id": zone, "timestamp": "2026-09-08T03:00:00+00:00"},
    ).json()

    assert peak["predicted_demand"] > off_peak["predicted_demand"]


def test_api_contract_stable_across_model_versions():
    """Checklist officielle (Guide §11) : 'Évolution du modèle sans casser
    le contrat API'.

    Le contrat (`zone, créneau, contexte -> demande, multiplicateur,
    confiance, modelVersion, validUntil`, cf. Guide §11) doit rester
    identique quel que soit le modèle utilisé derrière (baseline, XGBoost,
    une future v2). On vérifie ici le schéma OpenAPI exposé par le
    service : si un champ du contrat est renommé/supprimé par erreur lors
    d'une évolution du modèle, ce test échoue avant que le backend ne le
    découvre en intégration.
    """
    schema = app.openapi()
    response_schema = schema["components"]["schemas"]["PredictionResponse"]

    expected_fields = {
        "zone_id",
        "predicted_demand",
        "predicted_supply",
        "recommended_multiplier",
        "confidence",
        "model_version",
        "valid_until",
        "is_fallback",
        "explanation",
    }
    assert set(response_schema["properties"].keys()) == expected_fields

    required_fields = {
        "zone_id",
        "predicted_demand",
        "recommended_multiplier",
        "confidence",
        "model_version",
        "valid_until",
    }
    assert required_fields.issubset(set(response_schema.get("required", [])))

    # Le champ modelVersion doit toujours être une chaîne exploitable par
    # le backend pour tracer quelle version a produit une recommandation.
    payload = {
        "zone_id": _sample_zone(),
        "timestamp": datetime.now(timezone.utc).isoformat(),
    }
    body = client.post("/predict", json=payload).json()
    assert isinstance(body["model_version"], str) and body["model_version"]