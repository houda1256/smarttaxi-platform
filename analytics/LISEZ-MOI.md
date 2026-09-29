# Tableau de bord Taxi — projet Power BI Desktop

Ce dossier contient un projet Power BI Desktop complet et ouvrable : modèle de données,
131 mesures DAX et un rapport décisionnel de 5 pages. Toutes les données proviennent des
fichiers CSV du sous-dossier `Donnees`. Les montants sont exprimés en dinars tunisiens (TND).

Le rapport n'est pas un simple affichage de chiffres : chaque page confronte le réalisé à
la cible, mesure l'écart, rappelle le niveau de l'année précédente et se termine par un
bloc de lecture **Constat / Alerte / Action recommandée** rédigé automatiquement par des
mesures DAX. Les titres des visuels sont eux aussi dynamiques : ils changent de formulation
selon les données filtrées.

## 1. Ce qu'il faut avant de commencer

- **Power BI Desktop version d'octobre 2023 ou plus récente** (le format de projet `.pbip`
  n'existe pas dans les versions antérieures). La version gratuite du Microsoft Store
  suffit ; aucun compte payant n'est nécessaire pour ouvrir et actualiser le rapport.
- Deux options d'aperçu à activer **une seule fois** dans Power BI Desktop :
  1. Menu **Fichier → Options et paramètres → Options → Fonctionnalités en avant-première**
  2. Cocher **Enregistrer sous forme de projet Power BI (.pbip)** ainsi que
     **Format de fichier de rapport amélioré (PBIR)**
  3. Cliquer sur **OK** puis **fermer et relancer Power BI Desktop**

> Sans ces deux cases cochées, le fichier `.pbip` s'ouvre en erreur ou le rapport
> apparaît vide. C'est de loin la cause la plus fréquente de problème.

## 2. Installation en trois étapes

### Étape 1 — Copier le dossier

Décompressez l'archive et copiez le dossier `DashboardTaxi` à la racine du disque C :

```
C:\DashboardTaxi\
 ├─ Donnees\                     (les 15 fichiers CSV)
 ├─ DashboardTaxi.pbip           (le fichier à double-cliquer)
 ├─ DashboardTaxi.Report\        (les 5 pages du rapport)
 └─ DashboardTaxi.SemanticModel\ (tables, relations, mesures)
```

Le chemin `C:\DashboardTaxi` est celui prévu par défaut : en le respectant, vous n'avez
rien d'autre à configurer. Si vous préférez un autre emplacement, voir l'étape 3.

### Étape 2 — Ouvrir le projet

Double-cliquez sur **`DashboardTaxi.pbip`**. Power BI Desktop s'ouvre, charge le modèle
et affiche la page « Synthèse ». La première ouverture prend quelques secondes le temps
de lire les CSV.

### Étape 3 — (Uniquement si vous avez choisi un autre dossier)

Le projet lit les CSV via un paramètre unique, ce qui évite de corriger 15 requêtes :

1. Onglet **Accueil → Transformer les données → Transformer les données**
2. Dans le volet de gauche, ouvrez le dossier **Paramètres** et sélectionnez
   **`CheminDonnees`**
3. Remplacez la valeur courante par le chemin de **votre** dossier `Donnees`,
   par exemple `D:\Projets\DashboardTaxi\Donnees` (sans barre oblique finale)
4. **Fermer et appliquer**

## 3. Actualiser les données

Pour reprendre en compte des CSV modifiés : onglet **Accueil → Actualiser**.
Pour mettre à jour une seule table : clic droit sur la table dans le volet Données →
**Actualiser les données**.

Vous pouvez remplacer les CSV par vos propres extractions, à condition de conserver
exactement les mêmes noms de fichiers et les mêmes en-têtes de colonnes.

## 4. Contenu du rapport

| Page | Ce qu'elle répond |
|------|-------------------|
| **Synthèse** | Chiffre d'affaires réalisé, objectif, écart, taux d'atteinte, croissance sur 12 mois, nombre d'objectifs atteints ; trajectoire mensuelle réel / cible / année précédente ; contribution des villes comparée à leur cible ; tableau de pilotage de tous les indicateurs |
| **Activité et qualité** | Courses demandées, terminées, objectif et écart, taux de complétion, note client ; complétion mensuelle face à la cible ; saisonnalité par trimestre et par année ; volume × satisfaction × chiffre d'affaires par chauffeur |
| **Flotte et rentabilité** | Coût d'entretien, poids dans le chiffre d'affaires comparé au plafond cible, marge brute et taux de marge estimés, chiffre d'affaires par taxi ; postes d'intervention ; coût par marque ; marge par ville |
| **Clients** | Clients actifs, chiffre d'affaires des clients identifiés, dépense moyenne, part du chiffre d'affaires traçable, part payée par carte face à sa cible ; concentration du portefeuille |
| **Marketing** | Budget prévu et consommé, taux de consommation, poids du marketing dans le chiffre d'affaires face au plafond cible, campagnes actives ; annonceurs ; suivi campagne par campagne |

Sur chaque page :

- une rangée de six indicateurs en haut, systématiquement organisée **réalisé → cible →
  écart → taux d'atteinte** pour que la lecture soit immédiate ;
- deux repères de classement à gauche (le meilleur et le moins bon contributeur du
  périmètre filtré) ;
- des filtres à gauche (année, ville, et selon la page trimestre, statut de chauffeur,
  marque, garage, annonceur ou campagne), actifs sur tous les visuels de la page ;
- en bas, les trois cartes **Constat**, **Alerte** et **Action recommandée**, qui se
  recalculent avec les filtres et donnent la conclusion à retenir.

Le tableau de pilotage de la page Synthèse liste chaque indicateur suivi avec son réalisé,
sa cible, son écart, son taux d'atteinte, sa variation par rapport à l'année précédente et
son statut (atteint, à surveiller, en retard). Il est trié en mettant les indicateurs les
plus en retard en premier.

## 5. Structure du modèle

15 tables organisées en étoile autour de quatre tables de faits mensuelles
(courses, entretien, marketing, activité clients), reliées à un calendrier et aux
référentiels (ville, chauffeur, taxi, propriétaire, garage, client, annonceur, campagne),
complétées par les tables d'objectifs et d'hypothèses. 17 relations et 131 mesures DAX
nommées en français et formatées (TND, pourcentages, décimales), réparties en dossiers
d'affichage : indicateurs d'activité, financiers, flotte, clients, marketing, pilotage des
objectifs, comparaisons avec l'année précédente, ainsi que les mesures de rédaction qui
alimentent les titres dynamiques et les blocs Constat / Alerte / Action.

## 6. Point de méthode à connaître

La marge n'est pas issue d'une comptabilité fournie : elle est **estimée** en retirant du
chiffre d'affaires la rétrocession aux chauffeurs, le coût d'entretien et le budget
marketing consommé. Le taux de rétrocession (74 % du chiffre d'affaires) est une
hypothèse de travail, stockée dans `Donnees\hypotheses.csv`. Pour tester un autre taux,
modifiez la valeur dans ce fichier puis actualisez : toutes les mesures de marge suivent
automatiquement.

Les autres indicateurs sont calculés directement à partir des données sources, sans
estimation.

Les deux seuils qui déterminent le statut d'un indicateur (« à surveiller » en dessous de
98 % du taux d'atteinte, « hors cible » en dessous de 95 %) sont stockés dans
`Donnees\hypotheses.csv`. Les blocs Constat / Alerte / Action et les statuts du tableau de
pilotage s'appuient sur ces valeurs : les ajuster suffit à recalibrer la lecture, sans
toucher aux formules.

## 7. En cas de problème

| Symptôme | Cause et solution |
|----------|-------------------|
| Le fichier `.pbip` ne s'ouvre pas ou renvoie une erreur de format | Les options d'aperçu `.pbip` et PBIR ne sont pas activées, ou Power BI Desktop est trop ancien — voir la section 1 |
| « Erreur de format TMDL : InvalidLineType » sur un fichier de `tables` | Une ligne vide s'était glissée entre la description d'une table et sa déclaration ; corrigé dans cette version du projet. Si vous éditez vous-même un fichier `.tmdl`, ne laissez jamais de ligne vide juste après une ligne commençant par `///` |
| « Nous n'avons pas pu trouver le dossier » / erreur DataSource | Le paramètre `CheminDonnees` ne pointe pas vers le bon dossier — voir l'étape 3 |
| Le projet s'ouvre, les tables sont là, mais **aucun onglet de page** n'apparaît (canevas vide) | Power BI ignore la définition du rapport quand la case **Format de fichier de rapport amélioré (PBIR)** n'est pas cochée : activez-la (section 1) puis relancez Power BI Desktop et réouvrez le `.pbip` |
| Le rapport s'ouvre mais les visuels sont vides | Les tables ne sont pas encore chargées : lancez **Accueil → Actualiser** |
| Les colonnes ne correspondent plus après remplacement des CSV | Les en-têtes ont changé : rétablissez les noms de colonnes d'origine, ou adaptez la requête concernée dans l'éditeur Power Query |
| Les nombres s'affichent avec un séparateur inattendu | Le modèle est en français (fr-FR) ; l'affichage suit aussi les paramètres régionaux de Windows |

## 8. Corrections apportées dans cette version

- Les écarts entre deux taux (complétion, part de marché, part payée par carte, taux de
  marge) sont désormais exprimés correctement en points de pourcentage : ils affichaient
  systématiquement « 0,0 pt » à cause d'une mise à l'échelle manquante.
- L'indicateur **Croissance vs N-1** de la page Synthèse et l'indicateur de variation par
  rapport au mois précédent se calculent maintenant sur la période sélectionnée décalée de
  douze mois (ou d'un mois). Ils restaient vides dès que la sélection portait sur plus d'un
  mois, ce qui était le cas par défaut sur la page Synthèse.
- La carte **Action recommandée** de la page Clients renvoie toujours un texte, même quand
  le client le plus générateur ou la concentration du portefeuille ne peuvent pas être
  déterminés sur la sélection en cours.
- La ligne **Total** du tableau de pilotage de la page Synthèse est masquée : additionner
  des taux d'atteinte, des écarts et des variations n'avait aucun sens.

## 9. Enregistrer votre travail

Après modification, **Fichier → Enregistrer** met à jour les fichiers du projet
(dossiers `.Report` et `.SemanticModel`), sous forme de fichiers texte lisibles — ce qui
permet de les suivre dans un outil de gestion de versions. Pour obtenir un fichier unique
partageable, utilisez **Fichier → Enregistrer sous** et choisissez le format `.pbix`.
