# Modules métier — SmartTaxi

## 1. Objectif du document

Ce document décrit les onze modules métier du backend SmartTaxi, chacun destiné à être organisé comme une unité cohérente au sein du monolithe modulaire, avec des frontières claires vis-à-vis des autres modules (voir [docs/backend-architecture.md](backend-architecture.md)).

**Cette liste est la liste canonique**, issue du SmartTaxi Master Prompt (Partie 3). Elle remplace une précédente version de ce document dont les noms de modules (Customers, Drivers, Rides, Vehicles, Payments, Rewards) ne correspondent plus à la spécification métier actuelle.

Pour le détail complet de chaque module (entités, statuts, événements, règles métier précises), voir [docs/business-functional-specification.md](business-functional-specification.md), qui fait foi en cas de divergence avec ce document. Ce document-ci reste la carte concise des modules et de leurs interactions.

Les modules **Identity**, **Fleet** et **Ride** sont implémentés à ce jour, et le module **Payment** l'est partiellement (voir [docs/backend-audit-report.md](backend-audit-report.md) pour l'état d'Identity ; le rapport final de la Phase 3 couvre l'implémentation de Fleet ; le rapport final de la Phase 4 couvre l'implémentation de Ride ; le rapport final de la Phase 5 couvre le sous-ensemble de Payment implémenté) : les autres modules — et le reste du périmètre de Payment — restent à l'état de spécification, en préparation du développement à venir.

## 2. Identity

- **Objectif** : gérer l'identité et l'accès des utilisateurs de la plateforme, tous rôles confondus.
- **Responsabilités** : authentification, gestion des comptes, rôles multiples et permissions fines, émission/validation/rotation/révocation des tokens, vérification email/téléphone, 2FA, gestion de session, documents utilisateur, codes de parrainage.
- **Rôles supportés** : Customer, Driver, TaxiOwner, GaragePartner, RoadsideAssistancePartner, Advertiser, AdvertiserAdmin, PlatformAdmin, SuperAdmin, SupportAgent, FinanceManager, FleetManager, OperationsManager, ContentModerator, SecurityOfficer — un même compte peut cumuler plusieurs rôles.
- **Données sensibles** : identifiants de connexion, mots de passe (hachés), tokens, documents d'identité, données personnelles.
- **Interactions** : module transverse, utilisé par tous les autres modules pour authentifier et autoriser les actions.
- **Règles d'isolation** : aucun autre module ne stocke de logique d'authentification propre ; toute vérification d'identité passe par Identity.

## 3. Subscription

- **Objectif** : gérer les abonnements des acteurs éligibles (Customer, Driver, TaxiOwner, GaragePartner, RoadsideAssistancePartner, Advertiser, BusinessCustomer).
- **Responsabilités** : plans configurables (prix, période de facturation, essai, fonctionnalités, limites), activation, renouvellement, suspension, annulation, expiration, upgrade/downgrade, périodes de grâce, entitlements.
- **Données sensibles** : historique de facturation.
- **Interactions** : Identity (rôle cible du plan), Payment (facturation), Fleet/Advertising/Maintenance/RoadsideAssistance (fonctionnalités conditionnées par abonnement).
- **Règles d'isolation** : les vérifications d'accès à une fonctionnalité passent par un service d'entitlement dédié, jamais dispersées dans les contrôleurs.

## 4. Ride

- **Objectif** : gérer le cycle de vie complet d'une course (immédiate, planifiée, négociée, partagée).
- **Responsabilités** : recommandation de chauffeurs (score explicable), sélection manuelle obligatoire par le client, réponse du chauffeur, transitions de statut contrôlées, chat temporaire, suivi de localisation, annulation, complétion, SOS, notation.
- **Règle absolue** : le client choisit toujours manuellement son chauffeur ; le backend ne doit jamais assigner automatiquement un chauffeur.
- **Données sensibles** : géolocalisation, itinéraire, horodatage, contenu du chat.
- **Interactions** : Identity (client/chauffeur), Fleet (véhicule), Payment (règlement), Loyalty (gains de points), Notifications (alertes), Administration (SOS/litiges).
- **Règles d'isolation** : Ride orchestre le processus métier sans dupliquer les données détenues par Identity ou Fleet.
- **État d'implémentation** : implémenté (Domain/Application/Infrastructure/API + hub SignalR + migrations + tests unitaires et d'intégration Postgres). Détails complets, écarts assumés par rapport à la spécification et limitations connues dans le rapport final de la Phase 4 et dans `docs/business-functional-specification.md` (Module 3). Payment/Loyalty/Notifications n'existant pas encore, Ride prépare uniquement les points d'intégration (`AwaitingPayment`, événements documentés) sans écriture financière réelle.

## 5. Fleet

- **Objectif** : gérer le référentiel des propriétaires, flottes, véhicules et chauffeurs, ainsi que leurs affectations et contrats.
- **Responsabilités** : fiche véhicule, documents véhicule, affectations chauffeur-véhicule (avec historique et détection de conflits), contrats propriétaire-chauffeur, partage de revenus configurable, dépenses de flotte.
- **Données sensibles** : immatriculation, documents administratifs, identité du propriétaire.
- **Interactions** : Identity (chauffeur/propriétaire), Ride (véhicule utilisé), Maintenance (suivi technique), Payment (partage de revenus, dépenses).
- **Règles d'isolation** : Fleet est la source de vérité unique pour les données d'un véhicule ; les autres modules la référencent par identifiant. Un document véhicule expiré peut suspendre automatiquement le véhicule.
- **État d'implémentation** : implémenté (Domain/Application/Infrastructure/API + migrations + tests unitaires et d'intégration Postgres). Détails complets, limitations connues et décisions de sécurité dans le rapport final de la Phase 3. Le rattachement automatique d'un document véhicule expiré à la suspension du véhicule (ligne ci-dessus) n'est pas encore câblé en tâche planifiée — seul le balayage manuel (`expire-sweep`) existe à ce jour.

## 6. Maintenance

- **Objectif** : gérer le suivi technique et l'entretien des véhicules via des garages partenaires.
- **Responsabilités** : recommandation de garage (score explicable, sélection manuelle obligatoire), rendez-vous, devis, interventions, carnet d'entretien numérique (immuable sauf correction auditée), recommandations d'entretien (moteur à règles, pas de ML).
- **Données sensibles** : historique technique du véhicule, coûts d'intervention.
- **Interactions** : Fleet (véhicule concerné), RoadsideAssistance (suite d'une intervention d'urgence), Payment (facturation).
- **Règles d'isolation** : Maintenance ne modifie jamais directement les données d'identité du véhicule détenues par Fleet.

## 7. Roadside Assistance

- **Objectif** : gérer les interventions d'urgence en cas de panne ou d'incident sur la route.
- **Responsabilités** : réception des demandes, recommandation de partenaire (score explicable, sélection manuelle obligatoire), suivi de l'intervention, création d'incident pour les événements graves.
- **Règle absolue** : jamais d'assignation automatique d'un partenaire d'assistance.
- **Données sensibles** : géolocalisation en temps réel, données de sécurité liées à l'incident.
- **Interactions** : Ride (course interrompue), Fleet (véhicule concerné), Maintenance (transfert éventuel), Support (création d'incident).
- **Règles d'isolation** : RoadsideAssistance ne gère pas la facturation elle-même ; elle transmet les événements pertinents à Payment.

## 8. Loyalty

- **Objectif** : gérer la fidélité, les promotions et les parrainages.
- **Responsabilités** : deux types de points distincts (RewardPoints redeemables, StatusPoints déterminant le palier Bronze/Silver/Gold/Platinum), grand livre de points immuable, catalogue de récompenses, coupons/promotions, challenges, récompenses de parrainage.
- **Explicitement hors périmètre** : badges, portefeuille cashback, solde de crédit promotionnel.
- **Données sensibles** : historique de consommation associé à un profil utilisateur.
- **Interactions** : Identity, Ride (déclencheur de gains), Payment (aucun solde mutable sans écriture au grand livre).
- **Règles d'isolation** : toute opération de points passe par le grand livre immuable ; jamais d'écrasement direct d'un solde.

## 9. Payment (Finance, Invoicing, Cash Register)

- **Objectif** : fournir la fondation financière complète de la plateforme.
- **Responsabilités** : paiements (passerelle mock configurable), commissions configurables, partage de revenus chauffeur-propriétaire, comptes financiers internes, grand livre comptable immuable, remboursements, versements (payouts), factures, taxes, encaissements espèces, sessions de caisse, déclarations de caisse chauffeur, clients professionnels, litiges financiers, rapports.
- **Devise par défaut** : TND.
- **Données sensibles** : données de paiement, informations bancaires, historique des transactions.
- **Interactions** : quasiment tous les modules (Ride, Fleet, Maintenance, RoadsideAssistance, Subscription, Loyalty, Advertising) déclenchent des écritures financières.
- **Règles d'isolation** : Payment est le seul module autorisé à manipuler des données financières ; toute correction utilise une écriture d'extourne, jamais une suppression.
- **État d'implémentation** : un sous-ensemble ciblé est implémenté (Phase 5) — paiement de course (Cash/Card/CashAtAgency), statuts, facture et reçu générés à la confirmation, partage de revenus chauffeur-propriétaire basé sur les contrats Fleet, commission plateforme configurable, remboursements (total/partiel/annulation) audités, historique de transaction immuable, rapports de revenus. **Hors périmètre de la Phase 5** (non implémenté) : passerelle de paiement réelle, grand livre comptable général, comptes financiers internes, versements (payouts), sessions de caisse, déclarations de caisse chauffeur, clients professionnels, litiges financiers — ces éléments restent des spécifications pour une phase ultérieure. Détails complets dans le rapport final de la Phase 5 et dans `docs/business-functional-specification.md` (Module 9).

## 10. Advertising

- **Objectif** : gérer les campagnes publicitaires diffusées sur la plateforme (véhicules, écrans embarqués).
- **Responsabilités** : gestion des annonceurs et agences, campagnes, ciblage agrégé, validation des médias (scanner de sécurité mock), consentement propriétaire/chauffeur, sélection de véhicules, contrats et devis, installation et inspection, moteur de diffusion interne, partage de revenus, analytics agrégées, détection de fraude.
- **Données sensibles** : données contractuelles et de facturation des annonceurs ; jamais d'exposition de l'identité individuelle des passagers.
- **Interactions** : Payment (facturation, partage de revenus), Fleet (véhicules et consentement), Administration (validation des campagnes).
- **Règles d'isolation** : le ciblage et les analytics restent agrégés et privacy-aware.

## 11. Notifications, Communication, Support et Incidents

- **Objectif** : gérer les notifications, le support utilisateur et la gestion des incidents.
- **Responsabilités** : notifications in-app/email/SMS/push (abstractions) et SignalR, templates par événement/canal/langue/rôle, tickets de support (avec SLA, escalade, notes internes jamais visibles du demandeur), incidents (sévérité, investigation, preuves), escalade ticket → incident, réclamations.
- **Données sensibles** : contenu des tickets/incidents, preuves associées.
- **Interactions** : tous les modules peuvent déclencher une notification ou un ticket ; RoadsideAssistance/Ride (SOS) peuvent créer un incident directement.
- **Règles d'isolation** : les notes internes de support ne sont jamais exposées au demandeur ; les notifications de sécurité critiques ne sont jamais désactivables.

## 12. Administration, Audit, Configuration et Analytics

- **Objectif** : fournir aux administrateurs les outils de supervision, d'audit et de configuration de la plateforme.
- **Responsabilités** : rôles administratifs à moindre privilège (SuperAdmin, PlatformAdmin, SupportAgent, FinanceManager, AdvertiserAdmin, FleetManager, OperationsManager, ContentModerator, SecurityOfficer), actions sensibles nécessitant confirmation renforcée, tableaux de bord spécialisés par rôle, journal d'audit immuable, historique métier, paramètres système centralisés (`SystemSetting`), feature flags, références géographiques, règles de rétention, requêtes de données personnelles, analytics agrégées, rapports planifiés, exports, mode maintenance, endpoints de santé applicative.
- **Données sensibles** : accès transverse à des données sensibles de plusieurs modules — contrôle d'accès strict et journalisé.
- **Interactions** : potentiellement tous les modules, principalement en lecture, via des interfaces dédiées.
- **Règles d'isolation** : Administration ne contourne jamais les règles métier des autres modules ; toute action de supervision passe par les cas d'utilisation exposés par chaque module. Aucune infrastructure de monitoring externe n'est implémentée (Prometheus/Grafana/OpenTelemetry restent hors périmètre, réservés à une phase DevSecOps séparée).

## 13. Matrice des interactions entre modules

Lecture : une case cochée indique que le module en ligne interagit avec le module en colonne (dans le sens prévu des échanges).

| Module \ vers → | Identity | Subscription | Ride | Fleet | Maintenance | RoadsideAssistance | Loyalty | Payment | Advertising | Support | Administration |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Identity | — | | | | | | | | | | |
| Subscription | ✓ | — | | ✓ | ✓ | ✓ | | ✓ | ✓ | | |
| Ride | ✓ | | — | ✓ | | ✓ | ✓ | ✓ | | ✓ | |
| Fleet | ✓ | | ✓ | — | ✓ | ✓ | | ✓ | ✓ | | |
| Maintenance | | | | ✓ | — | ✓ | | ✓ | | | |
| RoadsideAssistance | | | ✓ | ✓ | ✓ | — | | ✓ | | ✓ | |
| Loyalty | ✓ | | ✓ | | | | — | ✓ | | | |
| Payment | | | | | | | | — | | | |
| Advertising | | | | ✓ | | | | ✓ | — | | ✓ |
| Support | ✓ | | ✓ | | | ✓ | | | | — | ✓ |
| Administration | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |

Cette matrice sera affinée au fur et à mesure de l'implémentation effective des modules ; voir [docs/business-functional-specification.md](business-functional-specification.md) pour la liste complète des événements métier inter-modules.
