"""
Point d'entrée du service — API de prédiction Demande & Tarification
dynamique, consommée par le backend .NET central (jamais directement par
les apps Flutter, cf. Guide d'intégration §4 et §11).

Lancer en local :
    uvicorn src.main:app --reload --port 8001

Endpoints :
    GET  /health   -> statut du service + version du modèle
    POST /predict  -> demande prédite + multiplicateur recommandé
"""

from __future__ import annotations

import logging
from datetime import datetime, timedelta, timezone

from contextlib import asynccontextmanager

from fastapi import FastAPI

from .config import SERVICE
from .model_baseline import BaselineModel
from .model_xgboost import DemandXGBModel
from .pricing import apply_context_adjustment, compute_multiplier
from .schemas import HealthResponse, PredictionRequest, PredictionResponse

logger = logging.getLogger("smarttaxi.pricing")
logging.basicConfig(level=logging.INFO)

# État du service en mémoire (pour la V0). En production, l'entraînement
# se fait hors-service (pipeline offline) et le modèle est chargé depuis
# un artefact versionné — voir README "Cycle de vie du modèle".
_state = {"baseline": BaselineModel(), "xgb": DemandXGBModel(), "trained": False, "data_source": "synthetic_in_memory"}


@asynccontextmanager
async def lifespan(_: FastAPI):
    _ensure_trained()
    yield


app = FastAPI(
    title="SmartTaxi — Demand & Dynamic Pricing Service",
    version=SERVICE.model_version,
    description=(
        "Service de recommandation (demande + multiplicateur). "
        "Ne modifie jamais Ride/Payment/Subscription. Aucun accès "
        "d'écriture à PostgreSQL. Le backend valide/plafonne toute sortie."
    ),
    lifespan=lifespan,
)


def _ensure_trained() -> None:
    """Entraîne les modèles au démarrage.

    Ordre de priorité de la source de données :
    1. Base de données SQLite (`data/smarttaxi_pricing.db`) si elle a été
       peuplée via `python seed_database.py` — simule un vrai pipeline
       d'ingestion qui lirait des courses individuelles et les agrégerait.
    2. Sinon, génération synthétique en mémoire (fenêtre figée) — mode par
       défaut, aucune installation supplémentaire requise.

    À REMPLACER en production par un chargement d'artefact déjà entraîné
    (cf. README, section "Prochaines étapes")."""
    if _state["trained"]:
        return

    try:
        from .database import is_seeded, load_demand_from_db

        if is_seeded():
            logger.info(
                "Base de données détectée (data/smarttaxi_pricing.db) — "
                "chargement et agrégation des courses individuelles."
            )
            df = load_demand_from_db()
            _state["data_source"] = "sqlite_database"
        else:
            raise RuntimeError("base non peuplée")
    except Exception as exc:  # noqa: BLE001 — retombe volontairement sur le mode synthétique
        logger.info(
            "Pas de base de données peuplée (%s) — entraînement sur données "
            "synthétiques en mémoire. Lancer `python seed_database.py` pour "
            "tester avec une vraie base.",
            exc,
        )
        from .data_synthetic import load_reference_dataset

        df = load_reference_dataset()

    ordered_df = df.sort_values("timestamp")
    cutoff = int(len(ordered_df) * 0.8)
    evaluation_baseline = BaselineModel().fit(ordered_df.iloc[:cutoff])
    baseline_preds = ordered_df.apply(
        lambda r: evaluation_baseline.predict_one(r.zone_id, r.hour, r.weekday), axis=1
    )

    try:
        metrics = _state["xgb"].fit(ordered_df, baseline_predictions=baseline_preds)
        logger.info("Comparaison vs baseline: %s", metrics.summary())
    except ImportError:
        logger.warning("xgboost non installé — le service fonctionnera uniquement en mode fallback baseline.")

    # Le fallback de production exploite ensuite tout l'historique disponible.
    _state["baseline"].fit(df)

    _state["trained"] = True


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        model_version=SERVICE.model_version,
        model_loaded=_state["xgb"].is_fitted(),
        data_source=_state["data_source"],
    )


@app.post("/predict", response_model=PredictionResponse)
def predict(request: PredictionRequest) -> PredictionResponse:
    _ensure_trained()

    hour = request.timestamp.hour
    weekday = request.timestamp.weekday()
    base_confidence = _state["baseline"].confidence_for(request.zone_id, hour, weekday)

    is_fallback = False
    try:
        if not _state["xgb"].is_fitted():
            raise RuntimeError("modèle XGBoost indisponible")
        predicted_demand = _state["xgb"].predict_one(request.zone_id, request.timestamp)
        confidence = _state["xgb"].confidence_for()
    except Exception as exc:  # noqa: BLE001 — fallback volontairement large
        logger.warning("Fallback sur la baseline (%s): %s", exc.__class__.__name__, exc)
        predicted_demand = _state["baseline"].predict_one(request.zone_id, hour, weekday)
        confidence = min(base_confidence, 0.5)
        is_fallback = True

    predicted_demand = apply_context_adjustment(
        predicted_demand, request.weather, request.is_event_nearby
    )

    multiplier = compute_multiplier(
        predicted_demand=predicted_demand,
        estimated_supply=request.available_drivers,
        previous_multiplier=request.previous_multiplier,
    )

    explanation = _build_explanation(request, predicted_demand, is_fallback)

    return PredictionResponse(
        zone_id=request.zone_id,
        predicted_demand=round(predicted_demand, 2),
        predicted_supply=request.available_drivers,
        recommended_multiplier=multiplier,
        confidence=confidence,
        model_version=SERVICE.model_version,
        valid_until=datetime.now(timezone.utc) + timedelta(seconds=SERVICE.prediction_validity_seconds),
        is_fallback=is_fallback,
        explanation=explanation,
    )


def _build_explanation(request: PredictionRequest, predicted_demand: float, is_fallback: bool) -> str:
    parts = [f"heure={request.timestamp.hour}h", f"jour={request.timestamp.strftime('%A')}"]
    if request.available_drivers is not None:
        parts.append(f"chauffeurs_disponibles={request.available_drivers}")
    if request.weather:
        parts.append(f"météo={request.weather.value}")
    if is_fallback:
        parts.append("mode=fallback_baseline")
    return ", ".join(parts)
