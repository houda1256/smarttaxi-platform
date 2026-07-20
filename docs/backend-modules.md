# Modules métier — SmartTaxi

## 1. Objectif du document

Ce document décrit les onze modules métier prévus pour le backend SmartTaxi. Chaque module est destiné à être organisé comme une unité cohérente au sein du monolithe modulaire, avec des frontières claires vis-à-vis des autres modules (voir [docs/backend-architecture.md](backend-architecture.md)).

Aucun de ces modules n'est encore implémenté : ce document décrit leur périmètre prévu, à titre de préparation pour le développement à venir.

## 2. Identity

- **Objectif** : gérer l'identité et l'accès des utilisateurs de la plateforme, tous rôles confondus.
- **Responsabilités** : authentification, gestion des comptes, gestion des rôles et permissions, émission et validation des tokens.
- **Fonctionnalités futures** : inscription, connexion, réinitialisation de mot de passe, gestion des rôles (client, chauffeur, propriétaire, garage, assistance routière, annonceur, administrateur), authentification multi-facteurs.
- **Données sensibles** : identifiants de connexion, mots de passe (hachés), tokens d'authentification, données personnelles d'identification.
- **Interactions** : module transverse, utilisé par tous les autres modules pour authentifier et autoriser les actions.
- **Règles d'isolation** : aucun autre module ne doit stocker de logique d'authentification propre ; toute vérification d'identité passe par Identity.

## 3. Customers

- **Objectif** : gérer les profils et le parcours des clients de la plateforme.
- **Responsabilités** : profil client, préférences, historique des courses (référence), gestion des adresses favorites.
- **Fonctionnalités futures** : gestion de profil, historique de courses, moyens de contact, préférences de trajet.
- **Données sensibles** : coordonnées personnelles, historique de déplacement, préférences.
- **Interactions** : Rides (demande de course), Payments (règlement des courses), Rewards (points de fidélité), Identity (authentification).
- **Règles d'isolation** : Customers ne doit pas accéder directement aux données de facturation détaillées de Payments ; il consomme des interfaces dédiées.

## 4. Drivers

- **Objectif** : gérer les profils et l'activité des chauffeurs.
- **Responsabilités** : profil chauffeur, statut de disponibilité, historique des courses effectuées, documents professionnels.
- **Fonctionnalités futures** : gestion de profil, disponibilité en temps réel, suivi des évaluations, gestion des documents (permis, assurance).
- **Données sensibles** : pièces d'identité, documents professionnels, coordonnées bancaires (référence), localisation.
- **Interactions** : Rides (affectation de courses), Vehicles (véhicule assigné), Payments (versements), Rewards, Identity.
- **Règles d'isolation** : Drivers ne gère pas directement les règles de paiement ; il transmet les événements nécessaires à Payments.

## 5. Rides

- **Objectif** : gérer le cycle de vie complet d'une course.
- **Responsabilités** : création de la demande, affectation d'un chauffeur, suivi du trajet, clôture de la course.
- **Fonctionnalités futures** : réservation immédiate ou planifiée, suivi en temps réel, historique, gestion des annulations.
- **Données sensibles** : géolocalisation du client et du chauffeur, itinéraire, horodatage des trajets.
- **Interactions** : Customers (demandeur), Drivers (exécutant), Vehicles (véhicule utilisé), Payments (règlement), Rewards (gains de points).
- **Règles d'isolation** : Rides orchestre le processus métier de la course sans dupliquer les données détenues par Customers, Drivers ou Vehicles.

## 6. Vehicles

- **Objectif** : gérer le référentiel des véhicules exploités sur la plateforme.
- **Responsabilités** : fiche véhicule, association à un propriétaire et/ou un chauffeur, statut opérationnel.
- **Fonctionnalités futures** : enregistrement de véhicule, documents (carte grise, assurance, contrôle technique), historique d'affectation.
- **Données sensibles** : immatriculation, documents administratifs, identité du propriétaire.
- **Interactions** : Drivers (chauffeur assigné), Maintenance (suivi technique), Rides (véhicule utilisé pour une course).
- **Règles d'isolation** : Vehicles est la source de vérité unique pour les données d'un véhicule ; les autres modules la référencent par identifiant.

## 7. Maintenance

- **Objectif** : gérer le suivi technique et l'entretien des véhicules.
- **Responsabilités** : planification des entretiens, historique des interventions, gestion des garages partenaires.
- **Fonctionnalités futures** : prise de rendez-vous, suivi des réparations, alertes d'entretien préventif.
- **Données sensibles** : historique technique du véhicule, coûts d'intervention.
- **Interactions** : Vehicles (véhicule concerné), RoadsideAssistance (suite d'une intervention d'urgence), Payments (facturation des interventions).
- **Règles d'isolation** : Maintenance ne modifie jamais directement les données d'identité du véhicule détenues par Vehicles ; elle référence le véhicule par identifiant.

## 8. RoadsideAssistance

- **Objectif** : gérer les interventions d'urgence en cas de panne ou d'incident sur la route.
- **Responsabilités** : réception des demandes d'assistance, affectation d'un prestataire, suivi de l'intervention.
- **Fonctionnalités futures** : déclenchement d'une demande d'urgence, géolocalisation de l'incident, suivi du prestataire.
- **Données sensibles** : géolocalisation en temps réel, données de sécurité liées à l'incident.
- **Interactions** : Rides (course en cours interrompue), Vehicles (véhicule concerné), Maintenance (transfert éventuel vers un entretien).
- **Règles d'isolation** : RoadsideAssistance ne prend pas en charge la facturation elle-même ; elle transmet les événements pertinents à Payments.

## 9. Payments

- **Objectif** : gérer les transactions financières de la plateforme.
- **Responsabilités** : facturation des courses, versements aux chauffeurs, facturation des interventions de maintenance ou d'assistance.
- **Fonctionnalités futures** : intégration d'un prestataire de paiement, gestion des factures, gestion des remboursements.
- **Données sensibles** : données de paiement, informations bancaires, historique des transactions.
- **Interactions** : Customers (paiement client), Drivers (versement), Rides, Maintenance, RoadsideAssistance, Advertising (facturation des campagnes).
- **Règles d'isolation** : Payments est le seul module autorisé à manipuler des données financières ; aucun autre module ne doit stocker de données de paiement.

## 10. Rewards

- **Objectif** : gérer le programme de fidélité de la plateforme.
- **Responsabilités** : attribution de points, gestion des avantages, historique de fidélité.
- **Fonctionnalités futures** : accumulation de points par course, échange de points contre des avantages, paliers de fidélité.
- **Données sensibles** : historique de consommation associé à un profil utilisateur.
- **Interactions** : Customers, Drivers (le cas échéant), Rides (déclencheur de gains de points).
- **Règles d'isolation** : Rewards ne doit pas dupliquer les données de profil détenues par Customers ou Drivers ; il les référence par identifiant.

## 11. Advertising

- **Objectif** : gérer les campagnes publicitaires diffusées sur la plateforme.
- **Responsabilités** : gestion des annonceurs, gestion des campagnes, diffusion des annonces.
- **Fonctionnalités futures** : création de campagnes, ciblage, suivi de diffusion, facturation des annonceurs.
- **Données sensibles** : données contractuelles des annonceurs, données de facturation.
- **Interactions** : Payments (facturation des campagnes), Administration (validation des campagnes).
- **Règles d'isolation** : Advertising ne doit pas accéder aux données personnelles des clients ou chauffeurs ; le ciblage, si prévu, s'appuie sur des données agrégées et anonymisées.

## 12. Administration

- **Objectif** : fournir aux administrateurs les outils de supervision de la plateforme.
- **Responsabilités** : supervision des utilisateurs, des courses, des véhicules et des campagnes ; gestion des règles de la plateforme.
- **Fonctionnalités futures** : tableaux de bord, gestion des litiges, modération des comptes, configuration de la plateforme.
- **Données sensibles** : accès transverse à des données sensibles de plusieurs modules, nécessitant un contrôle d'accès strict.
- **Interactions** : potentiellement tous les modules, en lecture principalement, via des interfaces dédiées.
- **Règles d'isolation** : Administration ne doit pas contourner les règles métier des autres modules ; toute action de supervision passe par les cas d'utilisation exposés par chaque module.

## 13. Matrice des interactions entre modules

Lecture : une case cochée indique que le module en ligne interagit avec le module en colonne (dans le sens prévu des échanges).

| Module \ vers → | Identity | Customers | Drivers | Rides | Vehicles | Maintenance | RoadsideAssistance | Payments | Rewards | Advertising | Administration |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Identity | — | | | | | | | | | | |
| Customers | ✓ | — | | ✓ | | | | ✓ | ✓ | | |
| Drivers | ✓ | | — | ✓ | ✓ | | | ✓ | ✓ | | |
| Rides | | ✓ | ✓ | — | ✓ | | ✓ | ✓ | ✓ | | |
| Vehicles | | | ✓ | | — | ✓ | ✓ | | | | |
| Maintenance | | | | | ✓ | — | ✓ | ✓ | | | |
| RoadsideAssistance | | | | ✓ | ✓ | ✓ | — | ✓ | | | |
| Payments | | | | | | | | — | | | |
| Rewards | | | | | | | | | — | | |
| Advertising | | | | | | | | ✓ | | — | |
| Administration | | | | | | | | | | ✓ | — |

Cette matrice sera affinée au fur et à mesure de l'implémentation effective des modules.
