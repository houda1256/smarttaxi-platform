"""
Générateur de données synthétiques.

IMPORTANT (statut prototype, cf. Guide §11) :
Ce module sert UNIQUEMENT à pouvoir développer, tester et démontrer le
pipeline avant d'avoir accès à un vrai flux de données du backend. Il ne
représente PAS la demande réelle à Tunis et ne doit jamais être présenté
comme tel dans le rapport final. Remplacer `load_historical_demand()` par
une vraie source (`database.load_demand_from_db`) dès que possible.

Deux couches composent la demande simulée :
1. Un PROFIL HORAIRE/HEBDOMADAIRE calibré sur de vraies données publiques
   (NYC Taxi 2013, cf. HOUR_WEIGHTS/WEEKDAY_WEIGHTS ci-dessous) — pas
   inventé à la main, mais géographiquement non pertinent pour Tunis (les
   habitudes de mobilité NYC ne sont pas celles de Tunis).
2. Des AJUSTEMENTS CALENDAIRES tunisiens documentés (Ramadan) appliqués
   par-dessus ce profil.

Ce mélange reste un PROXY, pas une prédiction de la vraie demande
tunisienne — à valider/remplacer avec de vraies données dès que
possible (cf. Guide §11 : "Les données publiques servent au
prototypage, pas à prétendre représenter la Tunisie sans validation").
"""

from __future__ import annotations

import random
from datetime import date, datetime, timedelta

import pandas as pd

from .config import SYNTHETIC_DATA
from .zoning import encode_geohash

# Quelques zones de démonstration autour du Grand Tunis (coordonnées
# approximatives, à remplacer par les zones réellement utilisées par
# l'équipe matching).
#
# zone_id = NOM LISIBLE (ex. "ariana"), pas un geohash — uniquement pour
# que la démo/soutenance soit compréhensible (graphiques, appels API
# lisibles). zone_id est traité comme une simple chaîne opaque partout
# dans le pipeline (groupby, dict, colonne SQLite TEXT) : le format n'a
# aucun impact sur le modèle.
#
# ⚠️ À NE PAS considérer comme le format d'intégration final : le Guide
# d'intégration (§11 "finaliser le découpage géographique commun" et §19)
# exige que le format réel de zone_id (geohash vs H3, précision) soit
# figé EN ÉQUIPE avec Rzeigui/Mohamed Amine (matching) et Eya (carte).
# `encode_geohash` reste disponible ci-dessous (`demo_zone_geohashes`)
# pour basculer facilement vers le format réel une fois décidé.
_DEMO_ZONES: dict[str, tuple[float, float]] = {
    "tunis_centre": (36.8065, 10.1815),
    "ariana": (36.8992, 10.1889),
    "lac": (36.8442, 10.2372),      # Lac / Berges du Lac
    "ben_arous": (36.7538, 10.2280),
    "la_marsa": (36.8781, 10.3247),
}


def demo_zone_ids(precision: int = 6) -> list[str]:
    """Retourne les zone_id de démonstration (noms lisibles).

    `precision` n'est plus utilisé pour construire l'id (conservé pour ne
    pas casser les appels existants) — voir `demo_zone_geohashes` si tu as
    besoin des geohash correspondants.
    """
    return list(_DEMO_ZONES.keys())


def demo_zone_coordinates() -> dict[str, tuple[float, float]]:
    """Coordonnées (lat, lon) de chaque zone de démonstration.

    Pratique pour calculer une distance (ex. Ariana -> Tunis centre) dans
    un script de test ou une démo.
    """
    return dict(_DEMO_ZONES)


def demo_zone_geohashes(precision: int = 6) -> dict[str, str]:
    """Mapping nom lisible -> geohash, pour comparer avec le format réel
    d'intégration une fois figé avec l'équipe matching/carte."""
    return {name: encode_geohash(lat, lon, precision) for name, (lat, lon) in _DEMO_ZONES.items()}


# ---------------------------------------------------------------------------
# Profil horaire/hebdomadaire calibré sur de VRAIES données publiques
#
# Source : NYC Taxi Trip Data 2013 (agrégat public, licence libre — via
# https://github.com/brandomr/nyc_taxis, données originales NYC TLC/Chris
# Whong), 171 976 998 courses réelles réparties par jour de semaine et
# heure. Poids normalisés à une moyenne de 1.0 (poids > 1 = heure/jour
# plus chargé que la moyenne).
#
# ⚠️ Reste un PROXY géographique : ce sont de vraies données de taxi,
# mais new-yorkaises, pas tunisiennes. Utile pour avoir une FORME de
# courbe réaliste (pics du soir plus marqués que le matin, weekend plus
# chargé) plutôt qu'une gaussienne inventée à la main — pas pour
# affirmer que "la demande à Tunis ressemble à ça".
# ---------------------------------------------------------------------------
HOUR_WEIGHTS: list[float] = [
    0.957, 0.707, 0.526, 0.384, 0.281, 0.241, 0.497, 0.871,
    1.080, 1.118, 1.084, 1.119, 1.179, 1.167, 1.203, 1.144,
    0.959, 1.169, 1.438, 1.505, 1.424, 1.395, 1.353, 1.199,
]  # index = heure 0..23

WEEKDAY_WEIGHTS: list[float] = [
    0.899, 0.998, 1.011, 1.035, 1.062, 1.062, 0.933,
]  # index = weekday 0=lundi .. 6=dimanche (Python datetime.weekday())

# Échelle pour retrouver un ordre de grandeur de demande comparable à
# l'ancienne formule (courses par créneau de 30 min et par zone).
_BASE_SCALE = 20.0


# ---------------------------------------------------------------------------
# Ajustement calendaire tunisien : Ramadan
#
# Dates 2026 confirmées officiellement (Mufti de la République, 17 février
# 2026) : 1er jour de Ramadan = jeudi 19 février 2026, Aïd al-Fitr = vendredi
# 20 mars 2026 (donc dernier jour de jeûne = jeudi 19 mars 2026).
#
# Effet modélisé (pattern largement documenté, PAS mesuré sur SmartTaxi) :
# pic de déplacements juste avant la rupture du jeûne (Iftar, autour du
# coucher du soleil ~18h-19h en cette période) et un second pic en soirée
# (sorties familiales/achats après l'Iftar, ~21h-23h). Les heures de pointe
# habituelles du matin sont en réalité réduites (horaires administratifs
# raccourcis pendant le Ramadan). À VALIDER avec de vraies données dès que
# possible — ne pas présenter ces coefficients comme mesurés.
# ---------------------------------------------------------------------------
RAMADAN_2026_START = date(2026, 2, 19)
RAMADAN_2026_END = date(2026, 3, 19)  # inclus


def is_ramadan(day: date, start: date = RAMADAN_2026_START, end: date = RAMADAN_2026_END) -> bool:
    return start <= day <= end


def ramadan_hour_multiplier(hour: int, is_ramadan_day: bool) -> float:
    """Multiplicateur additionnel appliqué à HOUR_WEIGHTS pendant le Ramadan.

    À discuter/calibrer en équipe (cf. en-tête du module) — ce ne sont pas
    des coefficients mesurés sur de vraies courses SmartTaxi.
    """
    if not is_ramadan_day:
        return 1.0
    if 17 <= hour <= 18:       # juste avant l'Iftar
        return 1.6
    if 21 <= hour <= 23:       # sorties après l'Iftar
        return 1.3
    if 8 <= hour <= 12:        # matinée : horaires raccourcis, moins de trajets
        return 0.75
    return 1.0


def load_historical_demand(
    start: datetime,
    end: datetime,
    freq_minutes: int = 30,
    precision: int = 6,
    seed: int = 42,
) -> pd.DataFrame:
    """Génère un historique synthétique demande/offre par zone et créneau.

    Le profil horaire/hebdomadaire de base est calibré sur de vraies
    données NYC Taxi 2013 (HOUR_WEIGHTS/WEEKDAY_WEIGHTS), avec un
    ajustement calendaire tunisien pour le Ramadan par-dessus — voir les
    docstrings ci-dessus pour les sources et limites de chaque couche.
    """
    random.seed(seed)
    zones = demo_zone_ids(precision)
    rows = []
    current = start

    while current <= end:
        hour = current.hour
        weekday = current.weekday()
        is_weekend = weekday >= 5
        ramadan_today = is_ramadan(current.date())

        base = (
            _BASE_SCALE
            * HOUR_WEIGHTS[hour]
            * WEEKDAY_WEIGHTS[weekday]
            * ramadan_hour_multiplier(hour, ramadan_today)
        )

        for i, zone in enumerate(zones):
            zone_factor = 0.6 + 0.15 * i  # certaines zones plus demandées
            noise = random.gauss(0, 3)
            demand = max(0.0, base * zone_factor + noise)

            # Offre simulée : corrélée à la demande moyenne historique,
            # avec une variance propre (chauffeurs qui se déplacent).
            supply = max(0.0, demand * random.uniform(0.6, 1.3))

            rows.append(
                {
                    "zone_id": zone,
                    "timestamp": current,
                    "hour": current.hour,
                    "weekday": current.weekday(),
                    "is_weekend": is_weekend,
                    "demand": round(demand, 2),
                    "supply": round(supply, 2),
                }
            )

        current += timedelta(minutes=freq_minutes)

    return pd.DataFrame(rows)


def load_reference_dataset() -> pd.DataFrame:
    """Charge LE dataset de démonstration figé (voir `config.SYNTHETIC_DATA`).

    À utiliser partout où on veut un dataset reproductible : le service
    (`main.py`), le notebook EDA, et tout script d'évaluation. N'utilise
    jamais `datetime.now()` — deux appels à cette fonction, à n'importe
    quel moment, quel que soit le jour d'exécution, retournent des lignes
    strictement identiques.
    """
    end = SYNTHETIC_DATA.reference_end
    start = end - timedelta(days=SYNTHETIC_DATA.history_days)
    return load_historical_demand(
        start=start,
        end=end,
        freq_minutes=SYNTHETIC_DATA.freq_minutes,
        seed=SYNTHETIC_DATA.seed,
    )