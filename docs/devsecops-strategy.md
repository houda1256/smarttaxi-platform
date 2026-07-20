# Stratégie DevSecOps — SmartTaxi

## 1. Objectif du document

Ce document présente la démarche DevSecOps prévue pour le backend SmartTaxi. Il distingue explicitement trois états pour chaque pratique :

- **Réalisé** : déjà en place dans le dépôt actuel.
- **Prévu à court terme** : décidé et planifié, mais pas encore implémenté.
- **À développer ultérieurement** : envisagé, sans engagement de calendrier précis à ce stade.

## 2. Sécurité dès la conception

**État : Réalisé (partiellement) / Prévu**

- Réalisé : le choix de la Clean Architecture isole les règles métier des détails techniques, ce qui limite la surface d'exposition des données sensibles à la seule couche Infrastructure.
- Réalisé : les modules métier sont pensés dès la conception avec des règles d'isolation (voir [docs/backend-modules.md](backend-modules.md)), afin de limiter la propagation d'un incident de sécurité à un seul module.
- Prévu : formalisation d'un modèle de menaces (threat model) par module sensible (Identity, Payments, Drivers).

## 3. Gestion des secrets

**État : Prévu**

- Aucun secret (mot de passe, token, chaîne de connexion, clé API) ne doit être présent dans le dépôt. Cette règle est déjà appliquée dans l'état actuel du code.
- Prévu : utilisation de variables d'environnement ou d'un gestionnaire de secrets (par exemple `dotnet user-secrets` en développement local, puis un coffre-fort de secrets en environnement de déploiement).
- Prévu : exclusion explicite des fichiers de configuration contenant des secrets via `.gitignore` (déjà en place pour les fichiers `.env`).

## 4. Validation des entrées

**État : Prévu**

- Toute entrée externe (requêtes HTTP, données utilisateur) devra être validée avant traitement, au niveau de la couche Application (validations des commandes/requêtes).
- Prévu : mise en place d'une bibliothèque de validation cohérente (par exemple FluentValidation) une fois les premiers cas d'utilisation développés.

## 5. Authentification et autorisation

**État : Prévu**

- L'authentification reposera sur des tokens JWT, gérés par le module Identity et implémentés dans SmartTaxi.Infrastructure.
- L'autorisation reposera sur les rôles applicatifs (client, chauffeur, propriétaire, garage, assistance routière, annonceur, administrateur).
- Prévu : mise en place des middlewares d'authentification/autorisation dans SmartTaxi.API, une fois le module Identity développé.

## 6. Tests automatisés

**État : Prévu**

- Les tests unitaires et d'intégration utiliseront xUnit, conformément aux conventions du projet.
- Prévu : création d'un projet de tests dédié, ciblant en priorité le Domain et l'Application (logique métier), puis l'Infrastructure et l'API.
- À développer ultérieurement : tests de bout en bout couvrant les parcours critiques (réservation d'une course, paiement).

## 7. Pipeline d'intégration continue (GitHub Actions)

**État : À développer ultérieurement**

- Aucun pipeline n'est actuellement configuré dans le dépôt.
- Prévu : un pipeline GitHub Actions exécutant, à minima, `dotnet restore`, `dotnet build` et `dotnet test` à chaque contribution.
- À développer ultérieurement : intégration progressive des étapes de sécurité décrites ci-dessous (analyse statique, dépendances, secrets, images Docker) au sein de ce même pipeline.

## 8. Analyse statique du code

**État : À développer ultérieurement**

- Non configurée à ce jour.
- Prévu : intégration d'un analyseur statique (par exemple les analyseurs Roslyn intégrés à .NET, complétés éventuellement par un outil dédié) pour détecter les problèmes de qualité et certaines vulnérabilités dès la compilation.

## 9. Analyse des dépendances

**État : À développer ultérieurement**

- Non configurée à ce jour.
- Prévu : audit régulier des packages NuGet utilisés (par exemple via `dotnet list package --vulnerable`), puis intégration de cette vérification dans le pipeline d'intégration continue.

## 10. Détection des secrets

**État : À développer ultérieurement**

- Non configurée à ce jour.
- Prévu : mise en place d'un outil de détection de secrets exécuté en pré-commit et/ou dans le pipeline CI, afin d'empêcher l'introduction accidentelle de secrets dans l'historique Git.

## 11. Scan des images Docker

**État : À développer ultérieurement**

- Aucune image Docker n'est actuellement définie pour le backend.
- Prévu, lorsque la conteneurisation sera mise en place : scan des images pour détecter les vulnérabilités connues dans les couches de base et les dépendances, avant toute publication.

## 12. Sécurisation future de Kubernetes

**État : À développer ultérieurement**

- Aucun déploiement Kubernetes n'est actuellement envisagé à court terme.
- À développer ultérieurement, si Kubernetes est retenu comme cible de déploiement : application des bonnes pratiques standards (moindre privilège des comptes de service, réseaux restreints entre espaces de noms, gestion des secrets via un mécanisme dédié type Kubernetes Secrets ou solution externe, politiques de sécurité des pods).

## 13. Monitoring et alertes

**État : À développer ultérieurement**

- Aucun outil de supervision n'est actuellement en place.
- Prévu à terme : journalisation structurée des applications, supervision de la disponibilité et des performances, alertes sur incidents de sécurité (échecs d'authentification répétés, erreurs anormales).

## 14. Étapes progressives de mise en œuvre

1. Finaliser l'architecture backend (réalisé) et la documenter (réalisé, ce document et les documents associés).
2. Mettre en place le projet de tests xUnit et les premiers tests du Domain.
3. Développer le module Identity avec authentification JWT et validation des entrées.
4. Mettre en place le pipeline GitHub Actions de base (restauration, compilation, tests).
5. Intégrer l'analyse des dépendances et la détection des secrets dans ce pipeline.
6. Ajouter l'analyse statique du code au pipeline.
7. Introduire la conteneurisation Docker avec scan d'image, lorsque le besoin de déploiement se précisera.
8. Étudier la sécurisation Kubernetes si cette cible de déploiement est retenue.
9. Mettre en place le monitoring et les alertes une fois les premiers modules déployés.

Cette progression sera ajustée en fonction de l'avancement réel du projet et des priorités décidées avec l'encadrant du stage.
