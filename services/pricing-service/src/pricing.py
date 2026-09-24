"""
Calcul du multiplicateur de tarification dynamique recommandé.

RAPPEL DE GOUVERNANCE (Guide §11) :
- Ce module produit une RECOMMANDATION. Le backend calcule/valide le prix
  officiel et persiste la transaction — jamais ce module.
- Le multiplicateur est TOUJOURS borné par PRICING.min_multiplier /
  PRICING.max_multiplier, mais ces bornes ne remplacent pas la validation
  backend : le backend doit pouvoir rejeter/plafonner à nouveau.
- Un lissage exponentiel évite les sauts de prix brutaux d'un appel à
  l'autre pour une même zone.
"""

from __future__ import annotations

from .config import PRICING
from .schemas import WeatherCondition


def apply_context_adjustment(
    demand: float,
    weather: WeatherCondition | None,
    is_event_nearby: bool | None,
) -> float:
    """Ajustement contextuel provisoire (règle métier, pas du ML).

    Les champs weather/is_event_nearby sont prévus dans le schéma API mais
    absents du modèle XGBoost faute de données réelles — cet ajustement
    documente leur effet en attendant un vrai entraînement.
    """
    if weather == WeatherCondition.rain:
        demand *= 1.15
    elif weather == WeatherCondition.extreme:
        demand *= 1.3
    if is_event_nearby:
        demand *= 1.2
    return demand


def compute_multiplier(
    predicted_demand: float,
    estimated_supply: float | None,
    previous_multiplier: float | None = None,
) -> float:
    """Retourne un multiplicateur recommandé, borné et lissé.

    Logique : plus le ratio demande/offre dépasse 1, plus le multiplicateur
    monte, avec une sensibilité configurable. Si l'offre est inconnue, on
    reste prudent et on ne surenchérit pas (multiplicateur = 1.0 par
    défaut, sujet à discussion en équipe).
    """
    if estimated_supply is None or estimated_supply <= 0:
        raw_multiplier = 1.0
    else:
        ratio = predicted_demand / estimated_supply
        if ratio <= PRICING.surge_start_ratio:
            raw_multiplier = 1.0
        else:
            raw_multiplier = 1.0 + PRICING.surge_sensitivity * (ratio - PRICING.surge_start_ratio)

    if previous_multiplier is not None:
        smoothed = (
            PRICING.smoothing_alpha * raw_multiplier
            + (1 - PRICING.smoothing_alpha) * previous_multiplier
        )
    else:
        smoothed = raw_multiplier

    return round(_clamp(smoothed, PRICING.min_multiplier, PRICING.max_multiplier), 3)


def _clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))
