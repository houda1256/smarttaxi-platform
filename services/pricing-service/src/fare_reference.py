"""
Référence tarifaire ILLUSTRATIVE — pour le rapport/la soutenance uniquement.

⚠️ CE MODULE N'EST PAS UTILISÉ PAR L'API (/predict). Il ne doit JAMAIS
l'être : le Guide d'intégration est explicite (§11) — ce service
recommande un multiplicateur, il ne calcule jamais le prix final. Le
backend est seul responsable du calcul/validation du prix
(`Payment`/`FinancialLedger`).

Ce module sert à répondre à une question différente : "concrètement, en
dinars, qu'est-ce que change mon multiplicateur ?" — utile pour illustrer
des résultats dans un rapport ou une démo, pas pour la production.

Sources (deux régimes tarifaires distincts, À CLARIFIER avec l'équipe
avant toute utilisation réelle — cf. Guide, principe "vérité actuelle vs
ancien document") :
  - "E-Taxi" (plateforme, 2026)  : Standard = 3,5 DT + 0,5 DT/km
                                    PRO      = 5 DT + 0,8 DT/km (min 12 DT)
  - Tarif réglementé officiel (déc. 2022) : 0,9 DT + 0,6 DT/km + 0,15 DT/min

Par défaut, ce module utilise le régime "E-Taxi" (le plus proche d'un
service de type SmartTaxi). À changer si l'équipe confirme un autre
régime de référence.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime


@dataclass(frozen=True)
class FareFormula:
    base_dt: float
    per_km_dt: float
    minimum_dt: float | None = None


STANDARD_FARE = FareFormula(base_dt=3.5, per_km_dt=0.5)
PRO_FARE = FareFormula(base_dt=5.0, per_km_dt=0.8, minimum_dt=12.0)

# Majorations FIXES et réglementaires — appliquées par le backend, JAMAIS
# par le multiplicateur de demande (pour éviter une double surcharge la
# nuit : le multiplicateur reflète l'offre/demande, pas l'heure en soi).
NIGHT_SURCHARGE_DT = 2.0  # 22h00 - 05h00
SUNDAY_HOLIDAY_SURCHARGE_DT = 1.0


def base_fare(distance_km: float, formula: FareFormula = STANDARD_FARE) -> float:
    """Tarif de base réglementaire, SANS le multiplicateur de demande."""
    fare = formula.base_dt + distance_km * formula.per_km_dt
    if formula.minimum_dt is not None:
        fare = max(fare, formula.minimum_dt)
    return round(fare, 2)


def is_night_surcharge_applicable(timestamp: datetime) -> bool:
    hour = timestamp.hour
    return hour >= 22 or hour < 5


def illustrative_fare_with_multiplier(
    distance_km: float,
    recommended_multiplier: float,
    timestamp: datetime,
    formula: FareFormula = STANDARD_FARE,
    is_sunday_or_holiday: bool = False,
) -> dict:
    """Combine tarif de base + majorations fixes + multiplicateur de demande.

    Retourne un détail (base, majorations, effet du multiplicateur) pour
    que le lecteur du rapport comprenne bien que ce sont des composantes
    SÉPARÉES — pas une boîte noire.

    ILLUSTRATIF UNIQUEMENT : ne remplace aucun calcul backend.
    """
    fare = base_fare(distance_km, formula)

    surcharges = 0.0
    if is_night_surcharge_applicable(timestamp):
        surcharges += NIGHT_SURCHARGE_DT
    if is_sunday_or_holiday:
        surcharges += SUNDAY_HOLIDAY_SURCHARGE_DT

    fare_with_surcharges = fare + surcharges
    fare_with_multiplier = round(fare_with_surcharges * recommended_multiplier, 2)

    return {
        "distance_km": distance_km,
        "base_fare_dt": fare,
        "fixed_surcharges_dt": round(surcharges, 2),
        "recommended_multiplier": recommended_multiplier,
        "illustrative_final_fare_dt": fare_with_multiplier,
    }
