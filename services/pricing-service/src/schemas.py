"""
Contrat d'API du service Demande & Tarification dynamique.

Ce contrat correspond exactement à ce que le Guide d'intégration attend en
§11 :
    "zone, créneau, contexte → demande prédite, multiplicateur recommandé,
     confiance, modelVersion, validUntil"

Le backend appelle POST /predict avec ces champs ; le service NE modifie
JAMAIS Ride/Payment/Subscription et n'écrit jamais dans PostgreSQL.
"""

from datetime import datetime, timezone
from enum import Enum
from typing import Optional

from pydantic import BaseModel, Field, field_validator


class WeatherCondition(str, Enum):
    clear = "clear"
    rain = "rain"
    extreme = "extreme"
    unknown = "unknown"


class PredictionRequest(BaseModel):
    # Identifiant de zone déjà calculé (geohash ou H3), fourni par le
    # backend ou dérivé de coordonnées — voir src/zoning.py.
    zone_id: str = Field(..., description="Identifiant de zone (geohash/H3), à figer avec l'équipe matching")
    timestamp: datetime = Field(..., description="Horodatage du créneau à prédire (UTC)")

    # Contexte optionnel — chaque variable DOIT être marquée optionnelle
    # tant que sa disponibilité réelle n'est pas confirmée par le backend
    # (cf. Guide §11).
    available_drivers: Optional[int] = Field(
        default=None, description="Nombre de chauffeurs disponibles dans la zone, si connu"
    )
    weather: Optional[WeatherCondition] = Field(default=None, description="Condition météo, si disponible")
    is_event_nearby: Optional[bool] = Field(default=None, description="Événement local connu à proximité")
    subscription_context: Optional[str] = Field(
        default=None,
        description="Contexte d'abonnement du client tel que renvoyé par le backend (jamais codé en dur ici)",
    )

    # Permet un lissage cohérent d'une requête à l'autre pour la même zone.
    previous_multiplier: Optional[float] = Field(
        default=None, description="Dernier multiplicateur connu pour cette zone (pour lissage)"
    )

    @field_validator("timestamp")
    @classmethod
    def ensure_utc(cls, v: datetime) -> datetime:
        if v.tzinfo is None:
            raise ValueError("timestamp doit être timezone-aware (UTC)")
        return v.astimezone(timezone.utc)


class PredictionResponse(BaseModel):
    zone_id: str
    predicted_demand: float = Field(..., description="Demande prédite (nombre de courses attendues sur le créneau)")
    predicted_supply: Optional[float] = Field(default=None, description="Offre estimée, si disponible")

    recommended_multiplier: float = Field(
        ..., description="Multiplicateur RECOMMANDÉ — le backend valide/plafonne/refuse avant application"
    )

    confidence: float = Field(..., ge=0.0, le=1.0, description="Confiance du modèle, 0 à 1")
    model_version: str
    valid_until: datetime = Field(..., description="Le backend ne doit plus utiliser cette prédiction après cette date")

    is_fallback: bool = Field(
        default=False, description="True si le modèle ML était indisponible et qu'une valeur de repli a été utilisée"
    )
    explanation: Optional[str] = Field(default=None, description="Facteurs principaux, pour explicabilité")


class HealthResponse(BaseModel):
    status: str
    model_version: str
    model_loaded: bool
    data_source: str = "synthetic_in_memory"
