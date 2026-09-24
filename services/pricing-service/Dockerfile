# Image du service SmartTaxi — Demande & Tarification dynamique.
#
# Ce conteneur contient UNIQUEMENT le service de prédiction (FastAPI).
# Il ne contient pas de base PostgreSQL : ce service n'accède jamais à la
# base opérationnelle (règle du Guide d'intégration §11). PostgreSQL est
# la responsabilité du backend .NET central, dans son propre conteneur.
#
# Construire :   docker build -t smarttaxi-pricing .
# Lancer :       docker run -p 8001:8001 smarttaxi-pricing

FROM python:3.12-slim

# Évite les fichiers .pyc et force l'affichage immédiat des logs
# (sinon les messages de démarrage restent bloqués dans un buffer).
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PYTHONPATH=/app

WORKDIR /app

# libgomp1 est requis par XGBoost (calcul parallèle OpenMP) — sans lui,
# l'import de xgboost échoue sur une image slim.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# Les dépendances sont copiées et installées AVANT le code source :
# tant que requirements.txt ne change pas, Docker réutilise cette couche
# en cache et ne réinstalle pas tout à chaque modification du code.
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/ ./src/
COPY seed_database.py .

# Base SQLite de démonstration : si elle est absente au moment du build,
# le service génère ses données en mémoire au démarrage (voir main.py).
# On la copie si elle existe, sans faire échouer le build sinon.
COPY data/ ./data/

EXPOSE 8001

# Vérification de santé : Docker saura si le service répond réellement,
# pas seulement si le processus est lancé. Le backend peut s'appuyer
# dessus pour attendre que le service soit prêt avant de l'appeler.
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8001/health')"

# --host 0.0.0.0 est OBLIGATOIRE dans un conteneur : avec la valeur par
# défaut (127.0.0.1), le service n'écouterait que depuis l'intérieur du
# conteneur et serait injoignable depuis la machine ou le backend.
CMD ["uvicorn", "src.main:app", "--host", "0.0.0.0", "--port", "8001"]
