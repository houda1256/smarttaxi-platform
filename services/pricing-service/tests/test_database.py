import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from src.database import (
    is_seeded,
    load_demand_from_db,
    seed_from_reference_window,
)

# IMPORTANT : chaque test reçoit `tmp_path` (fixture pytest, un dossier
# temporaire unique par test, auto-nettoyé). On construit un chemin de
# base DEDANS, jamais `data/smarttaxi_pricing.db` — pour ne JAMAIS risquer
# de vider la vraie base utilisée par le service pendant les tests.


def test_seeding_inserts_rows(tmp_path):
    db_path = tmp_path / "test.db"
    stats = seed_from_reference_window(db_path=db_path)
    assert stats["rides_inserted"] > 0
    assert stats["snapshots_inserted"] > 0
    assert is_seeded(db_path) is True


def test_seeding_is_reproducible(tmp_path):
    db_path1 = tmp_path / "a.db"
    db_path2 = tmp_path / "b.db"
    stats1 = seed_from_reference_window(db_path=db_path1)
    stats2 = seed_from_reference_window(db_path=db_path2)
    # Même graine, même fenêtre -> même nombre de lignes générées,
    # même en écrivant dans deux fichiers différents.
    assert stats1["rides_inserted"] == stats2["rides_inserted"]
    assert stats1["snapshots_inserted"] == stats2["snapshots_inserted"]


def test_load_demand_from_db_matches_pipeline_format(tmp_path):
    db_path = tmp_path / "test.db"
    seed_from_reference_window(db_path=db_path)
    df = load_demand_from_db(db_path=db_path)

    expected_columns = {"zone_id", "timestamp", "hour", "weekday", "is_weekend", "demand", "supply"}
    assert expected_columns.issubset(set(df.columns))
    assert len(df) > 0
    assert (df["demand"] >= 0).all()


def test_peak_hour_has_more_rides_than_off_peak(tmp_path):
    db_path = tmp_path / "test.db"
    seed_from_reference_window(db_path=db_path)
    df = load_demand_from_db(db_path=db_path)

    peak = df[df["hour"] == 18]["demand"].mean()
    off_peak = df[df["hour"] == 3]["demand"].mean()
    assert peak > off_peak


def test_load_demand_raises_if_not_seeded(tmp_path):
    db_path = tmp_path / "empty.db"
    try:
        load_demand_from_db(db_path=db_path)
        assert False, "devait lever une erreur car la base est vide/inexistante"
    except RuntimeError:
        pass
