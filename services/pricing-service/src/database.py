"""
Base de données de test — simule un flux de données "réel" (transactionnel)
au lieu du DataFrame déjà agrégé de `data_synthetic.py`.

Pourquoi : en vraie vie, le backend ne te donnera JAMAIS un tableau tout
prêt "demande par créneau". Il te donnera (via un export contrôlé, cf.
Guide d'intégration §11) des enregistrements individuels — une ligne par
course demandée, des instantanés de disponibilité chauffeurs — et c'est
TON pipeline qui doit les agréger. Ce module reproduit ce scénario avec
une base SQLite locale :

    rides                          -> une ligne par course demandée
    driver_availability_snapshots  -> une ligne par instantané de disponibilité

`load_demand_from_db()` agrège ensuite ces tables (comme le ferait un
vrai pipeline d'ingestion) pour reconstruire le même format
(zone_id, timestamp, demand, supply) que
`data_synthetic.load_reference_dataset()` — le reste du pipeline
(features, baseline, XGBoost) ne voit aucune différence.

⚠️ Les données insérées restent SYNTHÉTIQUES (même générateur que
data_synthetic.py) — seul le FORMAT DE STOCKAGE change, pas la véracité
des données. Statut : prototype, pour tester "comme en conditions
réelles" avant d'avoir accès à une vraie base.
"""

from __future__ import annotations

import math
import random
import sqlite3
from datetime import timedelta
from pathlib import Path

import pandas as pd

from .config import SYNTHETIC_DATA
from .data_synthetic import demo_zone_ids, HOUR_WEIGHTS, WEEKDAY_WEIGHTS, _BASE_SCALE, is_ramadan, ramadan_hour_multiplier

DEFAULT_DB_PATH = Path(__file__).resolve().parent.parent / "data" / "smarttaxi_pricing.db"
DB_PATH = DEFAULT_DB_PATH  # alias conservé pour compatibilité (seed_database.py, main.py)

SCHEMA = """
CREATE TABLE IF NOT EXISTS rides (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    zone_id TEXT NOT NULL,
    requested_at TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'completed'
);

CREATE TABLE IF NOT EXISTS driver_availability_snapshots (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    zone_id TEXT NOT NULL,
    snapshot_at TEXT NOT NULL,
    available_drivers INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_rides_zone_time ON rides(zone_id, requested_at);
CREATE INDEX IF NOT EXISTS idx_snap_zone_time ON driver_availability_snapshots(zone_id, snapshot_at);
"""


def get_connection(db_path: Path = DEFAULT_DB_PATH) -> sqlite3.Connection:
    db_path.parent.mkdir(parents=True, exist_ok=True)
    return sqlite3.connect(db_path)


def init_db(db_path: Path = DEFAULT_DB_PATH) -> None:
    with get_connection(db_path) as conn:
        conn.executescript(SCHEMA)


def is_seeded(db_path: Path = DEFAULT_DB_PATH) -> bool:
    try:
        with get_connection(db_path) as conn:
            row = conn.execute("SELECT COUNT(*) FROM rides").fetchone()
            return row[0] > 0
    except sqlite3.OperationalError:
        return False


def reset_db(db_path: Path = DEFAULT_DB_PATH) -> None:
    """Vide les tables — utile pour re-seeder proprement.

    ⚠️ Prend un `db_path` explicite précisément pour que les TESTS (qui
    appellent cette fonction) ne puissent jamais, par erreur, vider la
    vraie base `data/smarttaxi_pricing.db` utilisée par le service. Les
    tests doivent toujours passer un `db_path` pointant vers un fichier
    temporaire séparé (voir `tests/test_database.py`)."""
    init_db(db_path)
    with get_connection(db_path) as conn:
        conn.execute("DELETE FROM rides")
        conn.execute("DELETE FROM driver_availability_snapshots")
        conn.commit()


def _poisson(lam: float) -> int:
    """Tirage d'un entier suivant une loi de Poisson (algorithme de Knuth).

    Une vraie file de courses arrive de façon irrégulière (pas
    exactement "la moyenne toutes les X minutes") — Poisson est le
    modèle standard pour ce type d'arrivées aléatoires. Pas de
    dépendance à numpy/scipy : juste `random`, donc reproductible avec
    `random.seed()`.
    """
    threshold = math.exp(-lam)
    k = 0
    p = 1.0
    while True:
        k += 1
        p *= random.random()
        if p <= threshold:
            return k - 1


def seed_from_reference_window(freq_minutes: int = 30, db_path: Path = DEFAULT_DB_PATH) -> dict:
    """Peuple la base à partir du même profil de demande que
    `data_synthetic.py` (deux pics horaires, effet week-end), mais ici
    chaque course devient une VRAIE ligne individuelle avec son propre
    horodatage — comme une vraie table `rides`.

    Reproductible : graine fixe + fenêtre figée (`config.SYNTHETIC_DATA`),
    donc relancer ce script donne toujours le même nombre de lignes.
    """
    random.seed(SYNTHETIC_DATA.seed)
    reset_db(db_path)

    zones = demo_zone_ids(6)
    end = SYNTHETIC_DATA.reference_end
    start = end - timedelta(days=SYNTHETIC_DATA.history_days)

    rides_rows: list[tuple[str, str, str]] = []
    snapshot_rows: list[tuple[str, str, int]] = []
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
            zone_factor = 0.6 + 0.15 * i
            expected_demand = max(0.05, base * zone_factor)
            n_rides = _poisson(expected_demand)

            for _ in range(n_rides):
                offset_seconds = random.uniform(0, freq_minutes * 60)
                ts = current + timedelta(seconds=offset_seconds)
                rides_rows.append((zone, ts.isoformat(), "completed"))

            expected_supply = max(0, round(expected_demand * random.uniform(0.6, 1.3)))
            snapshot_rows.append((zone, current.isoformat(), expected_supply))

        current += timedelta(minutes=freq_minutes)

    with get_connection(db_path) as conn:
        conn.executemany(
            "INSERT INTO rides (zone_id, requested_at, status) VALUES (?, ?, ?)", rides_rows
        )
        conn.executemany(
            "INSERT INTO driver_availability_snapshots "
            "(zone_id, snapshot_at, available_drivers) VALUES (?, ?, ?)",
            snapshot_rows,
        )
        conn.commit()

    return {"rides_inserted": len(rides_rows), "snapshots_inserted": len(snapshot_rows)}


def load_demand_from_db(freq_minutes: int = 30, db_path: Path = DEFAULT_DB_PATH) -> pd.DataFrame:
    """Agrège les tables `rides` / `driver_availability_snapshots` en un
    DataFrame (zone_id, timestamp, hour, weekday, is_weekend, demand,
    supply) — même format que `data_synthetic.load_reference_dataset()`,
    mais calculé par agrégation à partir de données transactionnelles,
    comme un vrai pipeline d'ingestion le ferait.
    """
    if not is_seeded(db_path):
        raise RuntimeError(
            "Base non initialisée — lancer `python seed_database.py` avant "
            "d'utiliser load_demand_from_db()."
        )

    with get_connection(db_path) as conn:
        rides = pd.read_sql(
            "SELECT zone_id, requested_at FROM rides", conn, parse_dates=["requested_at"]
        )
        snapshots = pd.read_sql(
            "SELECT zone_id, snapshot_at, available_drivers FROM driver_availability_snapshots",
            conn,
            parse_dates=["snapshot_at"],
        )

    rides["bucket"] = rides["requested_at"].dt.floor(f"{freq_minutes}min")
    demand = rides.groupby(["zone_id", "bucket"]).size().reset_index(name="demand")

    snapshots = snapshots.rename(columns={"snapshot_at": "bucket", "available_drivers": "supply"})
    merged = demand.merge(snapshots, on=["zone_id", "bucket"], how="left")
    merged["supply"] = merged["supply"].fillna(merged["demand"])
    merged = merged.rename(columns={"bucket": "timestamp"})

    merged["hour"] = merged["timestamp"].dt.hour
    merged["weekday"] = merged["timestamp"].dt.weekday
    merged["is_weekend"] = merged["weekday"] >= 5

    return merged[["zone_id", "timestamp", "hour", "weekday", "is_weekend", "demand", "supply"]]