"""
Peuple la base de données locale (data/smarttaxi_pricing.db) avec des
courses individuelles simulées — à lancer une fois avant de tester le
service "comme en conditions réelles" (lecture depuis une vraie base
plutôt que génération en mémoire à chaque démarrage).

Usage :
    python seed_database.py
"""

from src.database import DB_PATH, seed_from_reference_window

if __name__ == "__main__":
    stats = seed_from_reference_window()
    print(f"Base peuplée : {DB_PATH}")
    print(f"  {stats['rides_inserted']} courses individuelles insérées (table `rides`)")
    print(
        f"  {stats['snapshots_inserted']} instantanés de disponibilité insérés "
        "(table `driver_availability_snapshots`)"
    )
    print("\nRedémarre le service (uvicorn src.main:app --reload --port 8001) :")
    print("il détectera automatiquement la base et l'utilisera au lieu des données en mémoire.")
