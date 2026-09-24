"""
Baseline simple : moyenne historique de la demande par (zone, heure,
jour de semaine). Le Guide d'intégration exige explicitement de comparer
toute approche ML à une baseline pour "démontrer le gain réel" (§11).

C'est aussi le modèle utilisé comme FALLBACK si XGBoost/Prophet est
indisponible (cf. contrat de fallback exigé par le backend, §11 et §20).
"""

from __future__ import annotations

import pandas as pd


class BaselineModel:
    def __init__(self):
        self._lookup: dict[tuple[str, int, int], float] = {}
        self._counts: dict[tuple[str, int, int], int] = {}
        self._global_mean: float = 0.0

    def fit(self, df: pd.DataFrame) -> "BaselineModel":
        grouped = df.groupby(["zone_id", "hour", "weekday"])["demand"].mean()
        self._lookup = grouped.to_dict()
        self._counts = df.groupby(["zone_id", "hour", "weekday"])["demand"].count().to_dict()
        self._global_mean = float(df["demand"].mean())
        return self

    def predict_one(self, zone_id: str, hour: int, weekday: int) -> float:
        return self._lookup.get((zone_id, hour, weekday), self._global_mean)

    def confidence_for(self, zone_id: str, hour: int, weekday: int, max_count: int = 8) -> float:
        """Confiance heuristique basée sur la quantité d'historique pour ce créneau."""
        if max_count <= 0:
            raise ValueError("max_count doit être strictement positif")
        n = self._counts.get((zone_id, hour, weekday), 0)
        return min(0.9, 0.3 + 0.6 * n / max_count)

    def is_fitted(self) -> bool:
        return bool(self._lookup)
