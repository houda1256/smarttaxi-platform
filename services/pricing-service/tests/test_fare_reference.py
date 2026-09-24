import sys
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from src.fare_reference import (
    PRO_FARE,
    STANDARD_FARE,
    base_fare,
    illustrative_fare_with_multiplier,
    is_night_surcharge_applicable,
)


def test_standard_fare_matches_published_formula():
    # 5 km en ville -> 3.5 + (5 * 0.5) = 6 DT (exemple du document source)
    assert base_fare(5, STANDARD_FARE) == 6.0


def test_pro_fare_respects_minimum():
    # trajet très court -> doit être plafonné au minimum de 12 DT
    assert base_fare(1, PRO_FARE) == 12.0


def test_night_surcharge_window():
    assert is_night_surcharge_applicable(datetime(2026, 1, 1, 23, 0, tzinfo=timezone.utc)) is True
    assert is_night_surcharge_applicable(datetime(2026, 1, 1, 4, 0, tzinfo=timezone.utc)) is True
    assert is_night_surcharge_applicable(datetime(2026, 1, 1, 14, 0, tzinfo=timezone.utc)) is False


def test_illustrative_fare_separates_components():
    result = illustrative_fare_with_multiplier(
        distance_km=10,
        recommended_multiplier=1.8,
        timestamp=datetime(2026, 1, 1, 18, 0, tzinfo=timezone.utc),  # pas de nuit
    )
    assert result["base_fare_dt"] == 8.5
    assert result["fixed_surcharges_dt"] == 0.0
    assert result["illustrative_final_fare_dt"] == round(8.5 * 1.8, 2)


def test_illustrative_fare_adds_night_surcharge_before_multiplier():
    result = illustrative_fare_with_multiplier(
        distance_km=10,
        recommended_multiplier=1.0,
        timestamp=datetime(2026, 1, 1, 23, 0, tzinfo=timezone.utc),
    )
    assert result["fixed_surcharges_dt"] == 2.0
    assert result["illustrative_final_fare_dt"] == 10.5  # (8.5 + 2) * 1.0
