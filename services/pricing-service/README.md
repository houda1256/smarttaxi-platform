# SmartTaxi — Module Demande & Tarification dynamique (Mariam Rabah)

Ce dépôt implémente ta partie du Guide d'intégration SmartTaxi (§11) :
un **service de recommandation** qui prédit la demande par zone/créneau et
propose un multiplicateur de tarification dynamique, consommé par le
backend .NET central — jamais directement par les apps Flutter.

## 0. Ce que tu dois retenir du Guide avant de coder

- Tu ne calcules **jamais** le prix final : tu **recommandes**. Le backend
  valide, plafonne, et persiste (`Payment`/`FinancialLedger`).
- Tu n'as **aucun accès en écriture** à PostgreSQL, idéalement pas d'accès
  direct du tout — le backend te fournit des exports/contrats de données.
- Ton API doit répondre au contrat exact :
  `zone, créneau, contexte → demande prédite, multiplicateur recommandé,
  confiance, modelVersion, validUntil`.
- Tu dois définir des **min/max et un lissage** validés avec le backend —
  le backend doit pouvoir rejeter un multiplicateur hors limites.
- Tu dois prévoir un **fallback** : si ton service tombe, le backend
  applique la tarification de base.
- Le découpage géographique (zones) doit être **le même** que celui utilisé
  par Rzeigui (éligibilité), Mohamed Amine (scoring) et Eya (carte) — à
  figer en réunion d'équipe.
- Documenter clairement **statut prototype vs production**, et les limites
  dues au manque de données réelles (tu n'as pas encore accès aux vraies
  données SmartTaxi).

## 1. Structure du projet

```
smarttaxi-pricing/
├── src/
│   ├── config.py          # bornes du multiplicateur, lissage, précision du zonage, version
│   ├── schemas.py         # contrat d'API (Pydantic) — zone/créneau/contexte → prédiction
│   ├── zoning.py          # découpage geohash (à aligner avec l'équipe matching)
│   ├── data_synthetic.py  # données de démo (PROTOTYPE — à remplacer par vraies données)
│   ├── features.py        # ingénierie de features
│   ├── model_baseline.py  # baseline (moyenne historique) = aussi le modèle de fallback
│   ├── model_xgboost.py   # modèle XGBoost + comparaison MAE/RMSE/R² vs baseline
│   ├── pricing.py         # calcul du multiplicateur (bornes + lissage)
│   ├── fare_reference.py  # traduction illustrative en DT (jamais appelée par l'API)
│   ├── database.py        # base SQLite de test (courses individuelles -> agrégation)
│   └── main.py            # API FastAPI : GET /health, POST /predict
├── seed_database.py       # peuple data/smarttaxi_pricing.db
├── data/
│   └── smarttaxi_pricing.db  # créé par seed_database.py (pas versionné habituellement)
├── tests/
│   ├── test_pricing.py    # bornes, lissage, cas limites, ajustement contexte
│   ├── test_model.py      # confiance baseline/XGBoost, encodage des features
│   ├── test_fare_reference.py  # formules tarifaires illustratives
│   ├── test_database.py   # seeding, agrégation, reproductibilité
│   └── test_api.py        # contrat API, fallback, heure de pointe, stabilité du contrat
├── notebooks/
│   ├── eda_demande_tarification.ipynb   # notebook EDA exécuté (graphiques inclus)
│   └── eda_demande_tarification.html    # export HTML, lisible sans Jupyter
├── build_eda_notebook.py  # régénère le notebook EDA (reproductible, voir §9)
├── requirements.txt
└── README.md
```

## 2. Installer et lancer

```bash
pip install -r requirements.txt --break-system-packages   # ou dans un venv
uvicorn src.main:app --reload --port 8001
```

Tester manuellement :

```bash
curl -X POST http://localhost:8001/predict \
  -H "Content-Type: application/json" \
  -d '{
        "zone_id": "sk3y0z",
        "timestamp": "2026-09-08T18:00:00Z",
        "available_drivers": 6
      }'
```

Documentation interactive auto-générée : `http://localhost:8001/docs`.

## 3. Contrat API

Le service expose **2 endpoints**. Port par défaut : **8001**.
Documentation interactive (Swagger) : `http://localhost:8001/docs`.

### GET /health — contrôle de santé

Vérifie que le service répond et que le modèle ML est chargé. Aucun calcul
de tarification.

**Réponse (200)** :

| Champ | Type | Description |
|-------|------|-------------|
| `status` | string | `"ok"` si le service tourne |
| `model_version` | string | Version du modèle (ex. `demand-pricing-v0.1.0`) |
| `model_loaded` | bool | `true` si XGBoost est entraîné ; `false` → fallback baseline |

Exemple :

```json
{
  "status": "ok",
  "model_version": "demand-pricing-v0.1.0",
  "model_loaded": true
}
```

### POST /predict — prédiction et multiplicateur recommandé

Endpoint principal, appelé par le backend .NET. Retourne une
**recommandation** — le backend valide/plafonne avant d'appliquer le prix.

**Corps de requête (JSON)** :

| Champ | Obligatoire | Type | Description |
|-------|-------------|------|-------------|
| `zone_id` | oui | string | Identifiant de zone (geohash ou H3), aligné avec l'équipe matching |
| `timestamp` | oui | datetime (UTC) | Créneau horaire à prédire |
| `available_drivers` | non | int | Chauffeurs disponibles dans la zone |
| `weather` | non | enum | `"clear"`, `"rain"`, `"extreme"`, `"unknown"` |
| `is_event_nearby` | non | bool | Événement local à proximité |
| `subscription_context` | non | string | Contexte abonnement fourni par le backend (jamais codé en dur ici) |
| `previous_multiplier` | non | float | Dernier multiplicateur connu (pour le lissage) |

**Réponse (200)** :

| Champ | Type | Description |
|-------|------|-------------|
| `zone_id` | string | Zone demandée |
| `predicted_demand` | float | Nombre de courses attendues sur le créneau |
| `predicted_supply` | float \| null | Offre (chauffeurs), si fournie en entrée |
| `recommended_multiplier` | float | Multiplicateur recommandé (borné 0.8–3.0, lissé) |
| `confidence` | float | Confiance du modèle, entre 0 et 1 |
| `model_version` | string | Version du modèle ayant produit la prédiction |
| `valid_until` | datetime (UTC) | Date limite d'utilisation de cette prédiction (120 s) |
| `is_fallback` | bool | `true` si le modèle ML était indisponible (baseline utilisée) |
| `explanation` | string \| null | Facteurs principaux (heure, météo, mode fallback…) |

Exemple de requête :

```json
{
  "zone_id": "sk3y0z",
  "timestamp": "2026-09-08T18:00:00Z",
  "available_drivers": 6
}
```

Exemple de réponse :

```json
{
  "zone_id": "sk3y0z",
  "predicted_demand": 35.5,
  "predicted_supply": 6.0,
  "recommended_multiplier": 3.0,
  "confidence": 0.75,
  "model_version": "demand-pricing-v0.1.0",
  "valid_until": "2026-09-08T15:33:29.337124Z",
  "is_fallback": false,
  "explanation": "heure=18h, jour=Tuesday, chauffeurs_disponibles=6"
}
```

**Erreurs** : payload invalide → `422 Unprocessable Entity`.

## 4. Lancer les tests

```bash
python -m pytest tests/ -v
```

Les 23 tests couvrent exactement les points de la checklist officielle
(Guide §11, "Tests d'intégration minimum") :

| Point de la checklist officielle | Test(s) |
|---|---|
| Zone à faible historique | `test_low_history_zone_still_returns_a_value` |
| Heure de pointe vs heure normale | `test_peak_hour_demand_higher_than_off_peak` |
| Service indisponible (fallback) | `test_predict_fallback_when_xgb_unavailable` |
| Multiplicateur extrême → plafonné/refusé | `test_multiplier_is_within_bounds`, `test_multiplier_never_above_max_even_with_extreme_ratio`, `test_multiplier_never_below_min` |
| Évolution du modèle sans casser le contrat API | `test_api_contract_stable_across_model_versions` |

Plus des tests complémentaires : payload invalide (422), timestamp sans
fuseau horaire rejeté, ajustement météo/événement, lissage du
multiplicateur, encodage des features, calibration de la confiance.

## 5. Cycle de vie du modèle (important pour ton rapport)

Au démarrage, le service s'entraîne automatiquement sur des **données
synthétiques de démonstration** (`data_synthetic.py`) — profil horaire
réaliste avec deux pics (matin/soir), effet week-end, bruit aléatoire.
**Ce n'est pas de la vraie demande tunisienne.** C'est volontaire : ça te
permet de développer et démontrer tout le pipeline dès maintenant, sans
attendre l'accès aux vraies données.

Pour passer à une vraie source de données, deux options, à documenter
dans ton rapport comme "prochaines étapes" :

1. **Dataset public** (déjà identifiés dans tes recherches : NYC Taxi Trip
   Data, Uber Movement) — bon pour valider le pipeline sur un vrai volume
   et une vraie saisonnalité, mais géographiquement non représentatif de
   Tunis. Remplace le contenu de `load_historical_demand()` par un
   chargement de ces fichiers (CSV/parquet), en gardant les mêmes colonnes
   (`zone_id`, `timestamp`, `demand`, `supply`).
2. **Export contrôlé du backend** (agrégats, pseudonymisés) — la vraie
   cible pour la version d'intégration finale. Demande à Houda un export
   read-only (nombre de courses par zone/créneau, chauffeurs disponibles
   par zone/créneau) plutôt qu'un accès direct à PostgreSQL.

En production, l'entraînement doit se faire **hors du service** (pipeline
offline planifié), et le service charge un artefact de modèle déjà
entraîné et versionné — pas un entraînement à chaque démarrage comme dans
cette V0 de démonstration.

### Reproductibilité (important)

Le service et le notebook EDA utilisent tous les deux
`src.data_synthetic.load_reference_dataset()`, qui repose sur une **fenêtre
temporelle figée** (`config.SYNTHETIC_DATA.reference_end` +
`history_days`) et une **graine aléatoire fixe** (`seed=42`). Avant, le
code appelait `datetime.now()` à chaque démarrage : la fenêtre
d'historique — et donc les métriques MAE/RMSE/R² et les graphiques —
changeait à chaque exécution, rendant impossible toute comparaison d'une
run à l'autre ou toute vérification par un autre membre de l'équipe.

Conséquence concrète : lancer `python build_eda_notebook.py` aujourd'hui
ou dans six mois produit exactement le même notebook, avec les mêmes
chiffres. Si tu changes intentionnellement la fenêtre ou la graine (par
exemple pour tester sur plus d'historique), documente le changement dans
ce README avec la date, pour que les métriques restent traçables.

## 6. Comparaison baseline vs XGBoost

`model_xgboost.py` calcule automatiquement le gain de MAE par rapport à la
baseline lors de l'entraînement (`EvalMetrics.summary()`). La comparaison
utilise un split temporel 80/20 : la baseline est entraînée uniquement sur
la fenêtre d'entraînement, puis le fallback est réentraîné sur tout
l'historique disponible. Cela évite de donner à la baseline accès aux valeurs
du holdout. Petit script
pour le voir toi-même :

```python
from src.data_synthetic import load_reference_dataset
from src.model_baseline import BaselineModel
from src.model_xgboost import DemandXGBModel

df = load_reference_dataset()  # fenêtre figée -> résultats reproductibles

ordered = df.sort_values("timestamp")
cutoff = int(len(ordered) * 0.8)
evaluation_baseline = BaselineModel().fit(ordered.iloc[:cutoff])
baseline_predictions = ordered.apply(
  lambda row: evaluation_baseline.predict_one(row.zone_id, row.hour, row.weekday),
  axis=1,
)
xgb = DemandXGBModel()
metrics = xgb.fit(ordered, baseline_predictions=baseline_predictions)
print(metrics.summary())
```

La confiance de la baseline dépend du nombre d'observations du créneau
(seuil réaliste de 8 observations), tandis que la confiance XGBoost est
calibrée sur le R² du holdout et plafonnée entre 0.4 et 0.9.

À inclure dans ton rapport/notebook EDA (§ "Livrables attendus avant
validation" du Guide : pipeline reproductible, EDA, modèle évalué +
métriques).

## 7. Notebook EDA (livrable Guide §11)

Le Guide exige explicitement un "notebook/rapport EDA" parmi les
livrables avant validation. Il est fourni dans `notebooks/` :

- `eda_demande_tarification.ipynb` — notebook exécuté, avec sorties et
  graphiques réels (pas un template vide).
- `eda_demande_tarification.html` — le même contenu, en HTML, pour le
  lire sans installer Jupyter (ouvrir directement dans un navigateur).

Contenu du notebook :

1. Distribution de la demande par heure (profil journalier, deux pics).
2. Distribution de la demande par zone.
3. Effet week-end / jour de semaine.
4. Carte heure × zone (heatmap).
5. Comparaison baseline vs XGBoost (MAE/RMSE/R², graphique en barres,
   split temporel 80/20 pour éviter toute fuite de données).
6. Prédit vs réel + distribution des résidus sur le holdout.
7. Section "Limites" — statut prototype, données synthétiques, zonage
   non figé, météo/événements non branchés au modèle (voir §8 ci-dessous
   pour le détail).

Pour régénérer le notebook après une modification du code (le pipeline
étant reproductible, cf. §5) :

```bash
python build_eda_notebook.py
jupyter nbconvert --to html notebooks/eda_demande_tarification.ipynb \
  --output eda_demande_tarification.html
```

## 8. Limites et statut des données — à lire avant toute intégration

Le Guide insiste explicitement sur ce point ("prochaines étapes",
"rapport sur les données manquantes et limites"). Résumé, détaillé dans
le notebook EDA (§9 du notebook) :

| Sujet | Statut actuel | Impact | Action avant intégration finale |
|---|---|---|---|
| **Source des données** | 100% synthétique (`data_synthetic.py`), générée par une formule connue | Les métriques (MAE/RMSE/R²) ne représentent PAS une performance réelle sur la demande tunisienne — elles valident seulement que le pipeline fonctionne | Brancher un dataset public (NYC Taxi, Uber Movement) pour prototyper à plus grande échelle, puis un export contrôlé du backend pour la version finale |
| **Historique** | 14 jours, 5 zones de démonstration | Pas de vraie saisonnalité (mois, jours fériés tunisiens, Ramadan, rentrée universitaire) captée | Étendre l'historique dès que des vraies données sont disponibles ; ré-évaluer le modèle sur plusieurs mois |
| **Découpage géographique (zonage)** | Geohash précision 6, choisi unilatéralement (`config.ZONING`) | Risque d'incohérence si Rzeigui/Mohamed Amine/Eya utilisent un découpage différent (cf. Guide §19, "Matrice des flux") | Réunion d'équipe pour figer geohash vs H3 et la précision — **bloquant avant intégration**, cf. Guide §22 |
| **Météo (`weather`)** | Champ accepté par l'API, mais **pas utilisé par le modèle XGBoost** — seulement un ajustement heuristique fixe (`pricing.apply_context_adjustment`, +15%/+30%) | Le multiplicateur bouge avec la météo, mais ce n'est pas un effet appris sur données réelles | Une fois une vraie source météo confirmée disponible côté backend, l'ajouter comme feature d'entraînement plutôt qu'une règle fixe |
| **Événements locaux (`is_event_nearby`)** | Idem : ajustement heuristique fixe (+20%), pas de feature ML | Approximation grossière, pas de granularité (taille/type d'événement) | Confirmer avec le backend si une vraie source d'événements existe avant d'investir dans une feature dédiée |
| **Confiance (`confidence`)** | Heuristique : quantité d'historique pour la baseline, R² du holdout pour XGBoost (plafonné 0.4–0.9) | Ce n'est pas une incertitude statistique rigoureuse (pas d'intervalle de prédiction) | À améliorer si le backend a besoin d'une confiance plus fine pour sa propre logique de validation |
| **Abonnement (`subscription_context`)** | Champ transmis tel quel par le backend, jamais interprété ni codé en dur ici | Aucun | Aucune action requise — respecte déjà la règle du Guide |

**Message clé pour le rapport final / la soutenance :** présenter ce
module comme un **pipeline validé et prêt à recevoir de vraies données**,
pas comme un modèle de production déjà évalué sur la demande réelle.
C'est cohérent avec le statut "prototype" que le Guide demande de garder
visible (§11).

## 9. Ce qu'il te reste à faire (checklist basée sur le Guide §11)

- [ ] Aligner le découpage de zone (`ZONING.geohash_precision` ou passage à
      H3) avec Rzeigui / Mohamed Amine / Eya — décision d'équipe,
      **bloquant** selon le Guide §22.
- [ ] Remplacer les données synthétiques par un vrai flux (dataset public
      pour prototyper, puis export backend pour l'intégration finale).
- [ ] Valider avec Houda les bornes `min_multiplier` / `max_multiplier` et
      le facteur de lissage — le backend doit avoir les mêmes valeurs ou
      des valeurs au moins aussi strictes.
- [ ] Décider avec le backend du format exact de `modelVersion` et de la
      politique de fallback côté backend (que fait-il si `is_fallback` est
      `true` ou si le service ne répond pas du tout).
- [x] Test "zone à faible historique" (`test_low_history_zone_still_returns_a_value`).
- [x] Test "heure de pointe vs heure normale" (`test_peak_hour_demand_higher_than_off_peak`).
- [x] Test "évolution du modèle sans casser le contrat API" (`test_api_contract_stable_across_model_versions`).
- [x] Notebook/rapport EDA (§7).
- [x] Pipeline reproductible — fenêtre de données figée (§5, "Reproductibilité").
- [x] Documenter le contrat d'API final (ce README §3 + `/docs` auto-généré)
      pour que Houda puisse l'intégrer côté backend sans lire tout le code.
- [ ] Une fois de vraies données disponibles : refaire tourner
      `build_eda_notebook.py` dessus et comparer au notebook de référence
      actuel (voir §8, ligne "Source des données").

## 10. Points de vigilance (rappel du Guide)

- Ne jamais écrire directement dans `Payment`/`FinancialLedger`.
- Ne jamais coder en dur des valeurs d'abonnement — elles viennent du
  backend.
- Toujours versionner : chaque réponse contient `model_version`, pour que
  le backend sache quel modèle a produit une recommandation.
- Garder le statut "prototype" bien visible tant que les vraies données ne
  sont pas branchées.

## 11. Référence tarifaire illustrative (`fare_reference.py`)

Module ajouté pour traduire un `recommended_multiplier` en impact
concret sur un tarif, en DT — utile pour le rapport/la soutenance, mais
**jamais appelé par l'API `/predict`**. La ligne à ne jamais franchir
reste celle du Guide §11 : ce service recommande, il ne calcule jamais
le prix final.

⚠️ **Point de vigilance non résolu** : deux régimes tarifaires
circulent dans la documentation collectée — un régime "plateforme"
(Standard 3,5 DT + 0,5 DT/km / PRO 5 DT + 0,8 DT/km) et un régime
"officiel réglementé" (0,9 DT + 0,6 DT/km + 0,15 DT/min, chiffres 2022).
Le module utilise le premier par défaut. **À confirmer avec l'équipe**
avant toute utilisation au-delà de l'illustration — ne pas trancher
seule.

Les majorations nuit (+2 DT, 22h–5h) et dimanche/jours fériés (+1 DT)
sont volontairement traitées comme des règles **fixes et
réglementaires**, additionnées avant le multiplicateur — elles ne
doivent jamais être recalculées ou dupliquées par le multiplicateur de
demande (sinon double surcharge la nuit).

## 12. Base de données de test — simuler des conditions réelles

Par défaut, le service génère ses données d'entraînement **en mémoire**
à chaque démarrage (`data_synthetic.py`). C'est pratique, mais ce n'est
pas réaliste : en production, tu ne recevras jamais un tableau déjà
agrégé "demande par créneau" — tu recevras (via un export contrôlé du
backend) des **enregistrements individuels** : une ligne par course
demandée, des instantanés de disponibilité chauffeurs. C'est à ton
pipeline de les agréger.

`src/database.py` simule ce scénario avec une vraie base **SQLite**
locale (`data/smarttaxi_pricing.db`), avec deux tables :

```
rides                          -- une ligne par course demandée
  id, zone_id, requested_at, status

driver_availability_snapshots  -- une ligne par instantané de disponibilité
  id, zone_id, snapshot_at, available_drivers
```

Les courses sont générées avec une loi de **Poisson** (arrivées
aléatoires autour d'une moyenne attendue) plutôt qu'un chiffre déjà
moyenné — comme un vrai flux de demande. Toujours des données
synthétiques au final (même profil horaire que `data_synthetic.py`),
mais le **format** est celui d'une vraie base transactionnelle.

### Utilisation

```bash
python seed_database.py
```

Redémarre ensuite le service normalement (`uvicorn src.main:app --reload
--port 8001`). Il détecte automatiquement la base et bascule dessus —
vérifiable via `GET /health` :

```json
{"status": "ok", "model_loaded": true, "data_source": "sqlite_database"}
```

Sans base peuplée (ou après `reset_db()`), `data_source` redevient
`"synthetic_in_memory"` — le service continue de fonctionner normalement,
juste avec les données générées à la volée.

### Inspecter la base manuellement

```bash
sqlite3 data/smarttaxi_pricing.db
sqlite> SELECT zone_id, COUNT(*) FROM rides GROUP BY zone_id;
sqlite> SELECT * FROM rides ORDER BY requested_at LIMIT 5;
```

### Pour aller plus loin

Remplace `seed_from_reference_window()` par un vrai import (CSV/export du
backend, ou dataset public type NYC Taxi) inséré dans les mêmes tables —
`load_demand_from_db()` n'a besoin d'aucune modification tant que le
schéma des deux tables reste le même.

