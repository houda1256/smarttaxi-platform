import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from src.pricing import compute_multiplier
from src.config import PRICING


def test_no_surge_when_supply_covers_demand():
    m = compute_multiplier(predicted_demand=10, estimated_supply=15)
    assert m == 1.0


def test_surge_when_demand_exceeds_supply():
    m = compute_multiplier(predicted_demand=30, estimated_supply=10)
    assert m > 1.0


def test_multiplier_never_below_min():
    m = compute_multiplier(predicted_demand=0, estimated_supply=100)
    assert m >= PRICING.min_multiplier


def test_multiplier_never_above_max_even_with_extreme_ratio():
    m = compute_multiplier(predicted_demand=100_000, estimated_supply=1)
    assert m <= PRICING.max_multiplier


def test_unknown_supply_defaults_to_neutral():
    m = compute_multiplier(predicted_demand=500, estimated_supply=None)
    assert m == 1.0


def test_smoothing_pulls_toward_previous_value():
    m_no_smoothing = compute_multiplier(predicted_demand=30, estimated_supply=10)
    m_smoothed = compute_multiplier(predicted_demand=30, estimated_supply=10, previous_multiplier=1.0)
    assert m_smoothed < m_no_smoothing


def test_context_adjustment_rain_increases_demand():
    from src.pricing import apply_context_adjustment
    from src.schemas import WeatherCondition

    base = 10.0
    adjusted = apply_context_adjustment(base, WeatherCondition.rain, None)
    assert adjusted == base * 1.15


def test_context_adjustment_event_increases_demand():
    from src.pricing import apply_context_adjustment

    base = 10.0
    adjusted = apply_context_adjustment(base, None, True)
    assert adjusted == base * 1.2
