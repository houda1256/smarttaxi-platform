import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from src.data_synthetic import load_reference_dataset
from src.features import build_feature_frame
from src.model_baseline import BaselineModel
from src.model_xgboost import DemandXGBModel


def _sample_df():
    # Fenêtre figée (config.SYNTHETIC_DATA) -> résultats reproductibles
    # d'une exécution à l'autre, quel que soit le jour où les tests tournent.
    return load_reference_dataset()


def test_baseline_mae_uses_real_baseline_not_global_mean():
    df = _sample_df()
    baseline = BaselineModel().fit(df)
    baseline_preds = df.apply(
        lambda r: baseline.predict_one(r.zone_id, r.hour, r.weekday), axis=1
    )

    metrics_real = DemandXGBModel().fit(df, baseline_predictions=baseline_preds)
    metrics_naive = DemandXGBModel().fit(df, baseline_predictions=None)

    assert metrics_real.baseline_mae != metrics_naive.baseline_mae


def test_baseline_confidence_scales_with_history():
    df = _sample_df()
    baseline = BaselineModel().fit(df)
    zone_id = df.iloc[0]["zone_id"]
    hour = int(df.iloc[0]["hour"])
    weekday = int(df.iloc[0]["weekday"])

    conf_known = baseline.confidence_for(zone_id, hour, weekday)
    conf_unknown = baseline.confidence_for("zzzzzz", hour, weekday)

    assert conf_known > conf_unknown
    assert 0.3 <= conf_known <= 0.9
    assert conf_unknown == 0.3


def test_xgb_confidence_comes_from_holdout_r2():
    df = _sample_df()
    model = DemandXGBModel()
    metrics = model.fit(df)

    assert model.confidence_for() == max(0.4, min(0.9, metrics.r2))


def test_weekday_is_encoded_as_category():
    frame = build_feature_frame(_sample_df().head(2))

    assert "weekday" not in frame.columns
    assert any(column.startswith("weekday_") for column in frame.columns)
