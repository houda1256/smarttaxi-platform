"""
Configuration centrale du module Demande & Tarification dynamique (Mariam Rabah).

Toutes les valeurs "métier" (min/max du multiplicateur, lissage, précision du
zonage) sont regroupées ici pour être facilement ajustées ET pour que le
backend puisse les faire correspondre à son propre contrat de validation
(le backend doit pouvoir plafonner/rejeter un multiplicateur hors limites,
cf. Guide d'intégration §11).
"""

from dataclasses import dataclass
from datetime import datetime, timezone


@dataclass(frozen=True)
class PricingConfig:
    # Multiplicateur de tarification dynamique — bornes dures.
    # Le backend est la dernière autorité : ces bornes sont une première
    # protection côté service IA, pas un remplacement de la validation backend.
    min_multiplier: float = 0.8
    max_multiplier: float = 3.0

    # Facteur de lissage exponentiel appliqué entre deux prédictions
    # successives pour la même zone, afin d'éviter des sauts de prix brutaux
    # d'une minute à l'autre. new = alpha * predicted + (1 - alpha) * previous
    smoothing_alpha: float = 0.35

    # Ratio demande/offre au-delà duquel on commence à augmenter le prix,
    # et en-deçà duquel on ne descend jamais sous 1.0 (pas de "sous-prix"
    # non validé par le backend).
    surge_start_ratio: float = 1.0

    # Sensibilité du multiplicateur au déséquilibre offre/demande.
    surge_sensitivity: float = 0.6


@dataclass(frozen=True)
class ZoningConfig:
    # Précision du geohash utilisé pour découper Tunis/le Grand Tunis en
    # zones. 6 caractères ≈ cellule de ~1.2km x 0.6km, une bonne base de
    # discussion avec Rzeigui / Mohamed Amine / Eya qui utilisent aussi des
    # coordonnées. À FIGER EN ÉQUIPE (cf. Guide §11, point "découpage
    # géographique commun").
    geohash_precision: int = 6


@dataclass(frozen=True)
class ServiceConfig:
    model_version: str = "demand-pricing-v0.1.0"
    # Durée de validité d'une prédiction avant qu'elle soit considérée comme
    # périmée par le backend.
    prediction_validity_seconds: int = 120


@dataclass(frozen=True)
class SyntheticDataConfig:
    """Fenêtre FIGÉE utilisée pour générer les données de démonstration.

    Reproductibilité : avant, le service et le notebook appelaient
    ``datetime.now()`` à chaque exécution, donc la fenêtre d'historique
    (et donc les métriques MAE/RMSE/R² et les graphiques EDA) changeait
    à chaque lancement — impossible à reproduire ou à comparer d'une run
    à l'autre. On fige donc ici une date de référence + une durée ; le
    générateur (`data_synthetic.load_historical_demand`) reste par
    ailleurs déterministe grâce à son paramètre `seed`, donc à partir de
    ces deux constantes, deux exécutions produisent EXACTEMENT le même
    dataset.

    À ne PAS avancer automatiquement — seulement changée volontairement,
    en notant la date du changement dans le README/rapport.
    """

    reference_end: datetime = datetime(2026, 9, 1, tzinfo=timezone.utc)
    history_days: int = 14
    freq_minutes: int = 30
    seed: int = 42


PRICING = PricingConfig()
ZONING = ZoningConfig()
SERVICE = ServiceConfig()
SYNTHETIC_DATA = SyntheticDataConfig()
