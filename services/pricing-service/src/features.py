"""
Construction des features à partir de l'historique demande/offre.

Toutes les features listées ici sont marquées comme disponibles ou
optionnelles — à confronter au dictionnaire de données commun que
proposera Aouidi Tassnim (qualité des données transverse, cf. Guide §17).
"""

from __future__ import annotations

import pandas as pd


CATEGORICAL_FEATURES = ["zone_id", "weekday"]
NUMERIC_FEATURES = ["hour", "is_weekend"]
TARGET = "demand"


def build_feature_frame(df: pd.DataFrame) -> pd.DataFrame:
    """Prépare un DataFrame prêt pour l'entraînement (encodage simple).

    On garde volontairement une approche simple (one-hot sur zone_id) pour
    la V0 du modèle : facile à expliquer, facile à comparer à la baseline.
    """
    frame = df.copy()
    frame["weekday"] = frame["weekday"].astype(str)
    frame["hour_sin"] = _cyclical_sin(frame["hour"], 24)
    frame["hour_cos"] = _cyclical_cos(frame["hour"], 24)
    frame = pd.get_dummies(frame, columns=CATEGORICAL_FEATURES, prefix=["zone", "weekday"])
    return frame


def _cyclical_sin(series: pd.Series, period: int) -> pd.Series:
    import numpy as np

    return np.sin(2 * np.pi * series / period)


def _cyclical_cos(series: pd.Series, period: int) -> pd.Series:
    import numpy as np

    return np.cos(2 * np.pi * series / period)


def feature_columns(frame: pd.DataFrame) -> list[str]:
    return [c for c in frame.columns if c not in ("timestamp", "demand", "supply")]
