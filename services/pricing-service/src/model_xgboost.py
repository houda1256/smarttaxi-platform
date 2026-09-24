"""
Modèle XGBoost pour la prédiction de demande.

Pourquoi XGBoost en V0 plutôt que Prophet : Prophet est excellent pour une
série temporelle par zone individuelle, mais nous voulons un seul modèle
multi-zones capable de généraliser (features "zone_id" en dummies).
Une comparaison Prophet (par zone) peut être ajoutée en V1 si l'équipe le
décide — l'API de prédiction (src/schemas.py) ne change pas, seul
l'intérieur de `train()` / `predict()` change, donc le contrat avec le
backend reste stable.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

from .features import build_feature_frame, feature_columns, TARGET


@dataclass
class EvalMetrics:
    mae: float
    rmse: float
    r2: float
    baseline_mae: float

    def summary(self) -> str:
        gain = self.baseline_mae - self.mae
        gain_pct = 100 * gain / self.baseline_mae if self.baseline_mae else 0.0
        return (
            f"MAE={self.mae:.2f} (baseline={self.baseline_mae:.2f}, "
            f"gain={gain:.2f} soit {gain_pct:.1f}%) RMSE={self.rmse:.2f} R2={self.r2:.3f}"
        )


class DemandXGBModel:
    def __init__(self):
        self._model = None
        self._columns: list[str] = []
        self._last_r2: float | None = None

    def fit(self, df: pd.DataFrame, baseline_predictions: pd.Series | None = None) -> EvalMetrics:
        import xgboost as xgb

        frame = build_feature_frame(df).sort_values("timestamp")
        if baseline_predictions is not None:
            baseline_predictions = baseline_predictions.reindex(frame.index)

        cols = feature_columns(frame)
        X = frame[cols]
        y = frame[TARGET]

        cutoff = int(len(frame) * 0.8)
        X_train, X_test = X.iloc[:cutoff], X.iloc[cutoff:]
        y_train, y_test = y.iloc[:cutoff], y.iloc[cutoff:]

        model = xgb.XGBRegressor(
            n_estimators=200,
            max_depth=5,
            learning_rate=0.08,
            subsample=0.9,
            colsample_bytree=0.9,
            random_state=42,
        )
        model.fit(X_train, y_train)

        preds = model.predict(X_test)
        mae = mean_absolute_error(y_test, preds)
        rmse = mean_squared_error(y_test, preds) ** 0.5
        r2 = r2_score(y_test, preds)

        if baseline_predictions is not None:
            baseline_mae = mean_absolute_error(y_test, baseline_predictions.loc[y_test.index])
        else:
            baseline_mae = mean_absolute_error(y_test, np.full_like(y_test, y_train.mean()))

        self._model = model
        self._columns = cols
        self._last_r2 = float(r2)
        return EvalMetrics(mae=mae, rmse=rmse, r2=r2, baseline_mae=baseline_mae)

    def is_fitted(self) -> bool:
        return self._model is not None

    def confidence_for(self) -> float:
        """Retourne une confiance bornée, calibrée sur la performance holdout."""
        if self._last_r2 is None:
            raise RuntimeError("Modèle non entraîné")
        return max(0.4, min(0.9, self._last_r2))

    def predict_one(self, zone_id: str, timestamp) -> float:
        if not self.is_fitted():
            raise RuntimeError("Modèle non entraîné — appeler fit() ou utiliser le fallback baseline.")

        row = pd.DataFrame(
            [
                {
                    "zone_id": zone_id,
                    "hour": timestamp.hour,
                    "weekday": timestamp.weekday(),
                    "is_weekend": timestamp.weekday() >= 5,
                }
            ]
        )
        frame = build_feature_frame(row)
        for col in self._columns:
            if col not in frame.columns:
                frame[col] = 0
        frame = frame[self._columns]
        pred = self._model.predict(frame)[0]
        return max(0.0, float(pred))
