# SmartTaxi — Business Functional Specification

**Source:** SmartTaxi Master Prompt, Part 3 ("Business Functional Specification"), provided 2026-07-29.
**Status:** Target specification for progressive implementation. Most of this is **not yet implemented** — see `docs/backend-audit-report.md` for Identity's audited state. **Module 4 (Fleet) and Module 3 (Ride Management) are fully implemented** (Phases 3 and 4); **Module 8 (Payments, Finance, Invoicing and Cash Register) is partially implemented** (Phase 5, ride-payment slice only) — see the "Implementation status" notes under each module below and the corresponding final reports for full detail; all other modules, and the unimplemented remainder of Module 8, remain specification-only.
**Purpose:** Authoritative reference for all future module work. This supersedes the module list and scope in `docs/backend-modules.md` (see reconciliation note below) wherever the two disagree.

> This document preserves the full specification as given, organized by module, so it survives independently of any single conversation and can be referenced during Plan/Implement phases for each module.

---

## Reconciliation note: module naming

`docs/backend-modules.md` (older, Part 1 of the Master Prompt) used: Identity, Customers, Drivers, Rides, Vehicles, Maintenance, RoadsideAssistance, Payments, Rewards, Advertising, Administration.

This specification (Part 3) supersedes that list with:

**Identity · Subscription · Ride · Fleet · Maintenance · Roadside Assistance · Loyalty (formerly "Rewards") · Payment/Finance/Invoicing/Cash Register (formerly "Payments") · Advertising · Notifications/Communication/Support/Incidents · Administration/Audit/Configuration/Analytics**

Notably: `Vehicles` → `Fleet` (broader: owners, fleets, vehicles, assignments, contracts, expenses), `Rewards` → `Loyalty`, `Payments` → a much larger `Payment, Finance, Invoicing and Cash Register` module, `Support`/`RoadsideAssistance` gain incident/complaint handling, and `Administration` absorbs audit, configuration, and analytics. `docs/backend-modules.md` should be revisited/rewritten to match once module work begins (tracked as an open decision, not yet actioned).

---

## Global business principles

SmartTaxi supports these actor types: Customers, Drivers, Taxi Owners, Vehicles, Fleets, Garages, Roadside Assistance Partners, Advertisers, Advertising Agencies, Business Customers, Cash Register Agents, Support Teams, Finance Teams, Security Teams, Platform Administrators.

**Nothing in this list may be hardcoded** — all must be configurable (via `SystemSetting` or equivalent): commissions, taxes, response delays, search radiuses, cancellation rules, loyalty point rules, document expiration reminders, payout thresholds, service availability, administrative thresholds.

---

## User and role model

- One central `User` entity; a user may hold multiple roles simultaneously (e.g. a TaxiOwner may also be a Driver).
- Roles: `Customer`, `Driver`, `TaxiOwner`, `GaragePartner`, `RoadsideAssistancePartner`, `Advertiser`, `AdvertiserAdmin`, `PlatformAdmin`, `SuperAdmin`, `SupportAgent`, `FinanceManager`, `FleetManager`, `OperationsManager`, `ContentModerator`, `SecurityOfficer`.
- Role assignment must never duplicate user identity data.
- **Permission-based authorization** layered on top of roles. Example permissions: `users.read`, `users.manage`, `users.suspend`, `drivers.approve`, `vehicles.manage`, `rides.monitor`, `payments.refund`, `payments.export`, `campaigns.approve`, `tickets.manage`, `incidents.manage`, `audit.read`, `settings.update`.

---

## Module 1 — Identity, Access and User Management

Registration, authentication, JWT access tokens, **refresh tokens with rotation**, token revocation, multiple roles, permission-based authorization, email/phone verification, password recovery/change, 2FA, session management, trusted devices, account suspension/reactivation/deletion requests, personal data export, preferences (language: `fr`/`ar`/`en` — technical names/code stay English), notification preferences.

**Registration**: Customer registration stays simple. Professional accounts (Driver, TaxiOwner, GaragePartner, RoadsideAssistancePartner, Advertiser, etc.) may require identity/legal/tax/company/bank information, documents, and manual approval. **Never activate a regulated professional role until all mandatory validation requirements are satisfied.**

**Referral code**: every account gets a unique `ReferralCode` (e.g. `ABCD123`), shareable, enterable at registration. **No reward for registration alone** — rewards are granted only after configurable conditions (email verified, phone verified, first completed ride, first valid payment, fraud checks passed). Sponsor may get reward points; referred user may get a welcome benefit. Fraud prevention required: no self-referral, no duplicate reward, device/phone/payment checks, suspicious-account review, configurable eligibility rules.

**User documents**: upload/validate/reject/expire/replace/version/manual-review/auto-status-update/admin-comments/secure-access/access-audit. Metadata: `DocumentType`, `OwnerId`, `FileReference`, `Status`, `IssueDate`, `ExpirationDate`, `ReviewedBy`, `ReviewedAt`, `RejectionReason`, `CreatedAt`. Statuses: `Pending`, `Approved`, `Rejected`, `Expired`, `Replaced`, `Suspended`. Owners: User, Driver, TaxiOwner, Vehicle, Garage, Roadside Partner, Advertiser, Business Customer.

**Fleet assignments** (Driver↔Vehicle): `DriverId`, `VehicleId`, `OwnerId`, `StartDate`, `EndDate`, `Status`, `Terms`, `RevenueSharingRule`, `CreatedBy`. Prevent conflicting active assignments; maintain history.

**Identity events**: `UserRegistered`, `UserEmailVerified`, `UserPhoneVerified`, `UserRoleAssigned`, `UserRoleRemoved`, `UserSuspended`, `UserReactivated`, `UserDocumentUploaded`, `UserDocumentApproved`, `UserDocumentRejected`, `UserSessionRevoked`, `ReferralConditionCompleted`, `ReferralRewardGranted`.

---

## Module 2 — Subscriptions

Subscribers: Customer, Driver, TaxiOwner, GaragePartner, RoadsideAssistancePartner, Advertiser, BusinessCustomer.

Configurable plans: `Name`, `Code`, `Description`, `TargetRole`, `Price`, `Currency` (default **TND**), `BillingPeriod`, `TrialPeriod`, `Features`, `Limits`, `IsActive`, `StartDate`, `EndDate`.

Supports: monthly/annual plans, trial, renewal, cancellation, suspension, expiration, upgrade, downgrade, grace periods, promotional offers, invoices, payment attempts, history. **Never delete historical subscriptions.**

Feature-based entitlement (vehicles count, drivers count, advertising features, analytics access, priority support, garage/roadside visibility, business invoicing, promotional benefits) — checked via a single entitlement service, **not scattered across controllers**.

Events: `SubscriptionCreated`, `SubscriptionActivated`, `SubscriptionRenewed`, `SubscriptionSuspended`, `SubscriptionCancelled`, `SubscriptionExpired`, `SubscriptionUpgraded`, `SubscriptionDowngraded`, `SubscriptionPaymentFailed`.

---

## Module 3 — Ride Management

**Implementation status (Phase 4, complete):** All scope areas below are implemented end-to-end (Domain/Application/Infrastructure/API + SignalR hub), with EF Core migrations, unit tests, and Postgres integration tests covering concurrency races and uniqueness constraints. Notable deltas from this spec, all deliberate:
- The status list is richer than this spec's illustrative one: 21 values (`Draft`, `Searching`, `DriversAvailable`, `DriverSelected`, `PendingDriverResponse`, `DriverAccepted`, `DriverRejected`, `DriverEnRoute`, `DriverArrived`, `PassengerOnBoard`, `InProgress`, `AwaitingPayment`, `Completed`, `CancelledByCustomer`, `CancelledByDriver`, `CancelledByAdmin`, `Expired`, `NoDriverAvailable`, `CustomerNoShow`, `DriverNoShow`, `Disputed`) — splitting `Cancelled` by actor and adding `AwaitingPayment` as the clean handoff point to a future Payments module, since no financial ledger entry is created by this module.
- Route convention: the repository has no `/api/v1` prefix anywhere (Identity and Fleet both use flat `/api/{module}` routes), so Ride follows the same convention (`/api/rides`, `/api/rides/driver`, `/api/admin/rides`, `/api/public/ride-share/{token}`) rather than introducing `/api/v1` unilaterally for one module.
- Driver hold duration reuses the same configurable value as the Driver response timeout (`IDriverSearchPolicy.DriverResponseTimeoutSeconds`) rather than a separate setting — a Driver's reservation should never outlive their own response window.
- `DriverProfile` (Fleet) gained three additive nullable fields (`LastKnownLatitude/Longitude/LastLocationRecordedAt`) plus one mutator, since Ride's driver search/recommendation needed a live-position source for available (not-yet-on-a-ride) Drivers that didn't exist in Identity or Fleet — the one Fleet change explicitly authorized for this phase.
- `DriverFirstName`/`ProfilePictureReference` in recommendation results reuse Identity's existing `UserPreferences.DisplayName`/`AvatarUrl` rather than adding new fields, since no separate "name" field exists anywhere in Identity or Fleet today.
- Real-time SignalR notification (`IRideRealtimeNotifier`, `/hubs/rides`) is fully built and wired for Driver location updates (the highest-frequency need). Broadcasting ride-status and shared-ride-status changes through the same Hub is implemented in the notifier and Hub themselves, but not yet called from the ~18 lifecycle/negotiation/shared-ride command handlers — wiring it in would touch nearly every existing Ride handler's constructor and, transitively, every test file that builds a Ride through those handlers, so it is documented here as follow-up work rather than a late, sweeping edit across already-green tests. Clients can poll `GetRideById`/`GetMyRidesForCustomer`/`GetMyRidesForDriver` for status in the meantime.
- No Payment, Loyalty, or Notifications module exists yet: `RideCompleted` prepares `FinalFare`/`AwaitingPayment` as the clean integration point but creates no ledger entry; `RideSafetyEvent`/`RideComplaint` are fully self-contained (no Incident/Support module to escalate into); shared-ride fare breakdowns are estimates only, with no Payment transaction created.
- Distance/route/fare/pricing/shared-ride-matching are deterministic, documented dev-time approximations (Haversine great-circle distance, an assumed 30 km/h average speed, time-of-day pricing bands) — no real map/routing provider or ML, per the spec's own constraint.

Central module. Supports immediate/scheduled/negotiated/shared rides, tracking, cancellation, completion, driver selection, temporary chat, emergency handling (SOS), complaints, ratings.

### Absolute driver-selection rule (mandatory, repeated for emphasis)
**The Customer ALWAYS chooses the Driver. SmartTaxi must never automatically assign a Driver.** The backend may find eligible drivers, score/sort them, and show ETA/rating/vehicle/fare/availability — but final selection is always manual. **Do not implement automatic driver assignment.** The same "user manually selects" rule applies identically to Garage selection (Module 5) and Roadside Assistance Partner selection (Module 6) — this is a cross-cutting, non-negotiable constraint, not specific to rides.

**Driver recommendation engine**: may use distance, ETA, availability, rating, vehicle category, requested services, completed-ride count, cancellation history, subscription/priority rules, compatibility. Score must be **explainable**. Customer gets a ranked list and chooses manually. Keep replaceable for future ML — **do not implement ML now**.

**Ride creation fields**: `CustomerId`, `PickupCoordinates`, `PickupAddress`, `DestinationCoordinates`, `DestinationAddress`, `RideType`, `VehicleCategory`, `PassengerCount`, `SpecialRequirements`, `ScheduledAt`, `EstimatedDistance`, `EstimatedDuration`, `EstimatedFare`, `PaymentMethod`, `Notes`. Validate coordinates, zone, availability, schedule, capacity, active customer, valid payment method, vehicle category.

**Ride lifecycle statuses**: `Draft`, `Searching`, `DriversAvailable`, `DriverSelected`, `PendingDriverResponse`, `DriverAccepted`, `DriverRejected`, `DriverArriving`, `DriverArrived`, `PassengerOnBoard`, `InProgress`, `Completed`, `Cancelled`, `Expired`, `Disputed`. All transitions governed by domain rules (no arbitrary status updates); maintain full `RideStatusHistory`.

**Driver response**: after Customer selects, Driver may accept/reject within a configurable timeout; rejection or expiration returns the Customer to the driver list — **no automatic reassignment to another driver**.

**Negotiated fare**: customer proposal → driver counter-offer → accept/reject, expiration, max negotiation rounds, full history. Final accepted price is immutable except via an authorized dispute/adjustment workflow.

**Shared rides**: opt-in, compatible route/schedule matching, capacity, detour limits, privacy protection, fare distribution, driver acceptance, passenger confirmation, cancellation rules. Never expose exact private locations between unrelated users before confirmation.

**Temporary chat**: Customer↔selected Driver, shared-ride participants (when authorized), Customer↔support. Linked to the Ride; starts after driver selection; closes/read-only after a configurable period post-completion/cancellation. Supports text, system messages, attachments (when allowed), read status, moderation flags, reporting, audited access under strict permissions.

**Location tracking**: real-time driver location via SignalR; track only when legally/operationally justified; retention is configurable; don't permanently store unnecessary high-frequency history.

**Cancellation**: captures `CancelledBy`, `Reason`, `Details`, `CancelledAt`, `RideStatusAtCancellation`, `Fee`, `RefundAmount`, `Penalty`, `AdministrativeDecision`. Policies configurable; different rules before/after driver acceptance.

**SOS**: may create a critical Incident, notify authorized security personnel, attach ride info + last known location, notify emergency contacts (if configured), preserve evidence, trigger high-priority notifications. **Do not auto-contact public emergency services unless a real authorized integration exists.**

**Rating**: post-completion, Customer↔Driver mutual rating, optional comment, moderation, one rating per ride/actor, configurable edit policy, abuse reporting.

**Events**: `RideRequested`, `EligibleDriversFound`, `DriverSelected`, `DriverRequestSent`, `DriverAcceptedRide`, `DriverRejectedRide`, `DriverArrived`, `RideStarted`, `RideCompleted`, `RideCancelled`, `RideExpired`, `RideFareProposed`, `RideFareCountered`, `RideFareAccepted`, `SharedRideMatchSuggested`, `SharedRideConfirmed`, `RideSOSActivated`, `RideDisputed`.

---

## Module 4 — Fleet, Vehicles and Taxi Owners

**Implementation status (Phase 3, complete):** All twenty scope areas below are implemented end-to-end (Domain/Application/Infrastructure/API), with EF Core migrations, unit tests, and Postgres integration tests covering ownership isolation, uniqueness, concurrency races, audit, and cross-fleet denial. Notable deltas from this spec, all deliberate:
- The `Fleet` entity is named `FleetOrganization` in code (still mapped to a `Fleets` table) to avoid clashing with the `SmartTaxi.Domain.Fleet` module namespace.
- Vehicle document expiration dates (`InsuranceExpiration`, `TechnicalInspectionExpiration`, `LicenseExpiration`) live on each `VehicleDocument` row, not as flat fields on `Vehicle` — avoids duplicating a date that a document already owns.
- Vehicle status is split into two independent dimensions: `VerificationStatus` (one-time platform gate: Pending/Approved/Rejected) and `OperationalStatus` (day-to-day: PendingVerification/Active/Assigned/InService/UnderMaintenance/Unavailable/Suspended/Retired/Rejected) — richer than this spec's single flat status list, kept in lockstep only at approve/reject time.
- `RevenueSharingRule` is implemented as contract-type-discriminated flat fields on the contract itself (`FixedAmount` for salary/rental types, `DriverPercentage`+`OwnerPercentage` summing to 100 for `PercentagePerRide`) rather than a separate value object — fully configurable per contract, never a hardcoded split.
- Contract activation has no real e-signature integration: it's a manual confirmation gated on a signed-document reference already attached before submission.
- The `VehicleDocumentExpired` → automatic vehicle suspension pipeline described below is **not wired as a scheduled job** — only a manual admin `expire-sweep` endpoint exists; no background scheduler was introduced (out of scope alongside the standing DevSecOps/infra exclusion).
- The residual race where two *different* assignments could overlap on the same vehicle/driver is application-checked but not closed at the database level (would need a Postgres `EXCLUDE` constraint over date range + day-of-week, deferred as a known limitation). All same-row status transitions (approve/activate/suspend/complete/cancel a *specific* assignment) are proven race-safe via Postgres integration tests.
- Domain creation events (`VehicleRegistered`, `DriverProfileCreated`, etc.) are genuinely raised and unit-tested; status-transition events are defined for documentation purposes but not dispatched — no outbox/event-dispatcher infrastructure was built.

Taxi Owners, Fleets, Vehicles, Drivers, Driver-Vehicle assignments, Owner-Driver contracts, vehicle documents, revenue sharing, fleet expenses, fleet analytics.

**Vehicle fields**: `OwnerId`, `FleetId`, `RegistrationNumber`, `VIN`, `Brand`, `Model`, `Year`, `Color`, `VehicleCategory`, `PassengerCapacity`, `FuelType`, `TransmissionType`, `CurrentMileage`, `Status`, `InsuranceExpiration`, `TechnicalInspectionExpiration`, `LicenseExpiration`. Statuses: `PendingApproval`, `Active`, `Inactive`, `UnderMaintenance`, `Suspended`, `ExpiredDocuments`, `Retired`.

**Vehicle documents**: registration certificate, insurance, technical inspection, taxi authorization, owner proof, configurable extras. An expired critical document **may automatically suspend the vehicle from accepting rides**.

**Driver-vehicle assignment**: temporary/permanent/scheduled, approval, termination, history, conflict detection, owner consent, driver consent where required. A Driver must not operate two active vehicles simultaneously unless an explicit validated business rule allows it.

**Owner-Driver contract**: validity period, fixed payment, percentage/mixed revenue sharing, expense responsibilities, minimum activity, penalties, termination rules, digital acceptance, attachments. **Revenue-sharing rules must be configurable — never a hardcoded universal split.**

**Fleet expenses**: fuel, maintenance, insurance, taxes, cleaning, penalties, admin costs, custom categories. Linkable to Vehicle/Driver/Fleet/Owner/Ride/Maintenance intervention.

**Events**: `VehicleRegistered`, `VehicleApproved`, `VehicleSuspended`, `VehicleDocumentExpired`, `DriverAssignedToVehicle`, `DriverUnassignedFromVehicle`, `OwnerDriverContractCreated`, `OwnerDriverContractTerminated`, `FleetExpenseRecorded`.

---

## Module 5 — Maintenance and Garages

Garage Partners, services, appointments, vehicle maintenance history, digital maintenance book, quotes, repairs, parts, invoices, payments, garage ratings, maintenance recommendations.

**Garage partner profile**: `BusinessName`, `LegalInformation`, `Address`, `Coordinates`, `ServiceZones`, `OpeningHours`, `SupportedVehicleCategories`, `AvailableServices`, `Equipment`, `Certifications`, `Rating`, `VerificationStatus`, `SubscriptionStatus`.

**Garage recommendation**: score by distance, ETA, availability, rating, supported service/vehicle, equipment, estimated cost, opening hours — **user manually chooses**, no auto-assignment; keep replaceable for future ML.

**Appointment statuses**: `Requested`, `PendingGarageResponse`, `Confirmed`, `QuotePending`, `QuoteSubmitted`, `QuoteAccepted`, `QuoteRejected`, `VehicleReceived`, `InProgress`, `WaitingForParts`, `Completed`, `Cancelled`, `Disputed`.

**Digital maintenance book**: per-vehicle immutable history (intervention date, mileage, garage, services, parts, cost, notes, attachments, invoice, next recommended maintenance, warranty) — immutable except via authorized correction with audit trail.

**Maintenance recommendations**: rule-based engine now (mileage, age, prior interventions, manufacturer schedule, detected issues, time since last service, recurring breakdowns); keep an abstraction for future ML — **do not implement ML**.

**Events**: `MaintenanceRequested`, `GarageSelected`, `MaintenanceAppointmentConfirmed`, `MaintenanceQuoteSubmitted`, `MaintenanceQuoteAccepted`, `MaintenanceStarted`, `MaintenanceCompleted`, `MaintenanceCancelled`, `MaintenanceRecommendationCreated`, `VehicleMaintenanceOverdue`.

---

## Module 6 — Roadside Assistance

Breakdown requests, towing, battery, tire replacement, fuel delivery, unlocking, mechanical assistance, accident assistance, configurable intervention types.

**Partner recommendation**: distance, ETA, availability, rating, equipment, intervention-type compatibility, supported vehicle category, service area — **user manually selects the partner, mandatory rule, no auto-assignment**.

**Request fields**: `RequesterId`, `VehicleId`, `RideId`, `Location`, `ProblemType`, `Description`, `Urgency`, `Attachments`, `PreferredPaymentMethod`, `RequestedAt`. Statuses: `Requested`, `PartnersAvailable`, `PartnerSelected`, `PendingPartnerResponse`, `Accepted`, `PartnerOnTheWay`, `PartnerArrived`, `InProgress`, `Completed`, `Cancelled`, `Rejected`, `Expired`, `Disputed`.

**Safety**: a serious request may create an Incident (accident, injury, dangerous location, suspicious behavior, repeated partner failure).

**Events**: `RoadsideAssistanceRequested`, `RoadsidePartnersRecommended`, `RoadsidePartnerSelected`, `RoadsidePartnerAccepted`, `RoadsidePartnerRejected`, `RoadsidePartnerArrived`, `RoadsideInterventionStarted`, `RoadsideInterventionCompleted`, `RoadsideAssistanceCancelled`.

---

## Module 7 — Loyalty, Promotions and Referrals

Two separate point concepts: `RewardPoints` (redeemable) and `StatusPoints` (determine tier: `Bronze`/`Silver`/`Gold`/`Platinum`). **Do not implement**: badges, cashback wallet, PromotionalCreditBalance.

**Point ledger** (immutable): `UserId`, `PointType`, `Amount`, `BalanceAfter`, `Reason`, `SourceType`, `SourceId`, `ExpirationDate`, `CreatedAt`, `CreatedBy`. **Never directly overwrite balances without ledger entries.**

**Earning rules**: completed rides, subscription activity, referral conditions, promotional campaigns, challenges, partner services, admin adjustment — configurable, with **idempotency to prevent duplicate attribution**.

**Reward catalog**: ride discount, free service, partner offer, subscription discount, promotional coupon — each with required points, eligibility, validity, quantity, target roles, usage limits, conditions.

**Promotions/coupons**: percentage/fixed discount, limited usage, first-use, city-based, service-based, user-segment, min amount, max discount, start/end dates. Prevent invalid stacking unless explicitly allowed.

**Challenges**: e.g. ride count, new-service use, referral, maintenance completion, shared-ride use — grant RewardPoints or eligible rewards.

**Events**: `RewardPointsGranted`, `RewardPointsRedeemed`, `RewardPointsExpired`, `StatusPointsGranted`, `LoyaltyTierChanged`, `RewardRedeemed`, `CouponApplied`, `ChallengeCompleted`, `ReferralRewardGranted`.

---

## Module 8 — Payments, Finance, Invoicing and Cash Register

**Implementation status (Phase 5, partial):** Only the ride-payment slice explicitly scoped by the Phase 5 prompt is implemented: `Payment`/`PaymentTransactionHistory`/`Invoice`/`Receipt`/`RefundRecord` entities, the `Pending→Authorized→Paid→{PartiallyRefunded,Refunded}` (or `Failed`/`Cancelled`) state machine, Cash/Card/CashAtAgency payment methods (this doc's fuller `CashAtGarage`/`CashToDriver`/`CashToPartner`/`BankTransfer`/`Disputed` are not implemented), driver-owner revenue sharing computed from Fleet's existing `DriverOwnerContract` (percentage/salary/rental/custom), a configurable platform commission, full/partial/cancellation refunds (audited), payment history and revenue/statistics reports. Notable deltas, all deliberate and driven by the Phase 5 prompt's own explicit exclusions:
- **No mock payment gateway** (`IPaymentGateway`) — the Phase 5 prompt explicitly excludes "payment gateway integration"; Card payments are recorded, not processed.
- **No financial ledger, internal financial accounts, or payouts** — out of scope per the Phase 5 prompt; revenue-sharing amounts are computed and stored on the `Payment` row itself, not posted to a double-entry ledger. This is a materially smaller model than this section's `Financial ledger`/`Internal financial accounts`/`Payouts` fields.
- **No cash register sessions or driver cash declarations** — out of scope; cash payments are recorded the same way as card payments (a `Payment` row), with no cash-drawer reconciliation.
- **No business customers, taxes catalog, or financial disputes** — out of scope; the Invoice's tax breakdown uses a single configurable `IInvoiceTaxPolicy.TaxPercentage` (default 0%), not the full taxes/jurisdiction model described below.
- **PDF generation is an abstraction only** (`IInvoicePdfGenerator`/`IReceiptPdfGenerator`) with a dev stub returning a deterministic storage key — no real PDF rendering library is integrated, per instructions.
- Ride's own `RidePaymentMethod` (a stated preference, Cash/Card/Wallet) is intentionally distinct from this module's `Payments.PaymentMethod` (the actual settlement method, Cash/Card/CashAtAgency) — see Module 3 above for the same reasoning already applied to Ride.

The remaining scope below (ledger, accounts, payouts, cash registers, business customers, taxes, financial disputes, export formats) remains specification-only, reserved for a future phase.

Default currency **TND**. Payment methods: `Card`, `Cash`, `CashAtAgency`, `CashAtGarage`, `CashToDriver`, `CashToPartner`, `BankTransfer`. **Use a mock payment gateway until a real provider is selected — do not implement a real payment provider.**

**Payment fields**: `PaymentNumber`, `PayerId`, `PayeeId`, `RelatedEntityType`, `RelatedEntityId`, `PaymentMethod`, `Amount`, `Currency`, `Status`, `ExternalReference`, `IdempotencyKey`, `CreatedAt`, `AuthorizedAt`, `CompletedAt`, `FailedAt`, `FailureReason`. Statuses: `Pending`, `Authorized`, `Completed`, `Failed`, `Cancelled`, `Refunded`, `PartiallyRefunded`, `Disputed`.

**Mock payment gateway**: `IPaymentGateway` abstraction; mock supports configurable outcomes (success, failure, timeout, duplicate callback, delayed confirmation).

**Commission engine**: configurable by service type, role, city, vehicle category, subscription, campaign, date range, contract, payment method — **never one universal hardcoded commission**.

**Driver-Owner revenue sharing**: percentage/fixed/mixed split per active contract, deductions, expenses, configurable priority order; every calculation traceable.

**Internal financial accounts**: Platform, Driver, TaxiOwner, Garage, RoadsidePartner, Advertiser, BusinessCustomer, CashRegister — **never untraceable mutable balances; all movements via ledger entries.**

**Financial ledger** (immutable, double-entry style): `TransactionNumber`, `DebitAccount`, `CreditAccount`, `Amount`, `Currency`, `TransactionType`, `RelatedEntityType`, `RelatedEntityId`, `Description`, `CreatedAt`, `CreatedBy`. **Never delete; corrections use reversal entries.**

**Payouts**: `BeneficiaryId`, `Amount`, `Currency`, `Period`, `PaymentMethod`, `BankReference`, `Status`, `ApprovedBy`, `PaidAt`. Statuses: `Draft`, `PendingApproval`, `Approved`, `Processing`, `Paid`, `Failed`, `Cancelled`. Configurable minimum payout amount.

**Refunds**: full/partial/administrative/automatic-eligible/rejection/history; require reason, permission, audit, idempotency; important refunds may need enhanced confirmation.

**Cash payments**: collected by Driver/Garage/Roadside Partner/Agency/Cash Register — capture expected/declared/received cash, difference, declaration date, validator, dispute, settlement.

**Cash register sessions**: `CashRegisterId`, `AgentId`, `OpeningBalance`, `OpenedAt`, `ClosingBalance`, `ExpectedBalance`, `ActualBalance`, `Difference`, `ClosedAt`, `Status`. Movements: `CashIn`, `CashOut`, `PaymentCollection`, `Refund`, `Correction`, `Expense`, `Deposit`. Closing must be auditable.

**Driver cash declarations**: period, related rides, expected/declared amount, difference, validation, dispute, settlement.

**Invoices**: ride/subscription/advertising/maintenance/roadside/business/grouped. Fields: `InvoiceNumber`, `Issuer`, `Recipient`, `Lines`, `Subtotal`, `TaxAmount`, `DiscountAmount`, `TotalAmount`, `Currency`, `IssueDate`, `DueDate`, `Status`, `PaymentStatus`. **Never silently modify finalized invoices — use credit notes/correction documents.**

**Business customers**: organization profile, employees, authorized riders, cost centers, ride policies, spending limits, grouped billing, delayed payments, monthly invoices, account manager, contracts.

**Taxes**: configurable rate, type, validity period, service applicability, jurisdiction, exemption rules.

**Financial disputes**: fare, cash amount, commission, payout, invoice, refund, partner payment — maintain evidence/decisions/audit history.

**Financial reports**: revenue, commission, payouts, refunds, unpaid invoices, cash differences, partner earnings, customer spending, taxes, ledger extracts. Export formats: PDF, Excel, CSV.

**Events**: `PaymentCreated`, `PaymentCompleted`, `PaymentFailed`, `PaymentRefunded`, `CommissionCalculated`, `RevenueSplitCalculated`, `LedgerTransactionPosted`, `PayoutRequested`, `PayoutApproved`, `PayoutCompleted`, `InvoiceIssued`, `InvoicePaid`, `CashDeclared`, `CashRegisterOpened`, `CashRegisterClosed`, `FinancialDisputeOpened`.

---

## Module 9 — Advertising

Advertisers, Advertising Agencies, advertiser admins, Campaigns, physical taxi advertising, in-vehicle screens, vehicle selection, media validation, contracts, quotes, invoices, payments, analytics, fraud detection, complaints.

**Advertiser profile**: `BusinessName`, `LegalName`, `TaxIdentifier`, `Address`, `Contacts`, `BillingInformation`, `VerificationStatus`, `AgencyId`. Agencies may manage multiple advertisers.

**Campaign fields**: `CampaignName`, `AdvertiserId`, `AgencyId`, `Objective`, `StartDate`, `EndDate`, `Budget`, `Targeting`, `Channels`, `Media`, `SelectedVehicles`, `Status`, `FrequencyCap`, `PricingModel`. Statuses: `Draft`, `Submitted`, `UnderReview`, `ChangesRequested`, `Approved`, `Scheduled`, `Active`, `Paused`, `Suspended`, `Completed`, `Cancelled`, `Rejected`.

**Channels**: vehicle exterior, vehicle interior, taxi screens, multi-channel, future digital channels via abstraction.

**Targeting**: city, zone, vehicle category, operating hours, route categories, estimated audience, schedule. **Never expose individual passenger identities to advertisers — analytics stay aggregated/privacy-aware.**

**Media validation**: upload, format/size validation, manual review, content moderation, security scanner abstraction (`IFileSecurityScanner`, mock implementation — no real external scanning infra), approval/rejection/replacement/versioning.

**Vehicle consent**: Taxi Owner must consent before a vehicle joins physical/screen advertising; driver consent configurable by campaign type/contract; consent history stored.

**Vehicle selection**: city, zone, category, availability, owner/driver consent, active status, campaign compatibility, prior campaign conflicts.

**Contracts and quotes**: quote, proposal, negotiation, digital acceptance, validity, pricing, channels, vehicles, installation responsibilities, revenue sharing, cancellation rules.

**Installation and inspection**: appointment, proof/photos, installer, inspection, approval, removal appointment, damage report.

**Ad serving engine** (internal, for taxi screens): active campaign, approved media, target zone, schedule, frequency cap, vehicle eligibility, impression limits. **No external ad tech.**

**Advertising revenue sharing**: configurable between Platform/TaxiOwner/Driver/Agency/other eligible partner — all distributions create ledger entries.

**Advertising analytics** (aggregated): active vehicles, estimated impressions, screen displays, campaign reach, geographic distribution, budget usage, installation status, campaign performance, partner earnings.

**Advertising fraud detection**: impossible impression counts, repeated duplicate events, inactive-vehicle reporting, manipulated screen events, conflicting installations — flag for review.

**Events**: `AdvertiserRegistered`, `CampaignCreated`, `CampaignSubmitted`, `CampaignApproved`, `CampaignRejected`, `CampaignActivated`, `CampaignPaused`, `CampaignSuspended`, `CampaignCompleted`, `VehicleAddedToCampaign`, `VehicleConsentGranted`, `AdvertisingInstalled`, `AdvertisingInspectionApproved`, `AdImpressionRecorded`, `AdvertisingRevenueDistributed`.

---

## Module 10 — Notifications, Communication, Support and Incidents

**Notifications**: in-app, email/SMS/push abstractions, real-time SignalR. Categories: Ride, Payment, Subscription, Fleet, Maintenance, Roadside, Loyalty, Advertising, Support, Security, Administrative. Users configure preferences where legally/operationally allowed; **critical security notifications cannot always be disabled.**

**Templates**: by event, channel, language, role, application; variables must be validated.

**Support tickets**: `TicketNumber`, `RequesterId`, `Category`, `Subject`, `Description`, `Priority`, `RelatedEntityType`, `RelatedEntityId`, `AssignedAgentId`, `Status`, `CreatedAt`, `ResolvedAt`. Statuses: `Open`, `Assigned`, `InProgress`, `WaitingForCustomer`, `Resolved`, `Closed`, `Reopened`. Supports messages, attachments, assignment, internal notes (**never exposed to requester**), resolution, reopening, CSAT rating, notifications, full history, configurable SLA, escalation, dashboard metrics.

**Incidents**: `IncidentNumber`, `Type`, `Severity`, `Title`, `Description`, `ReportedBy`, `RelatedEntityType`, `RelatedEntityId`, `Latitude`, `Longitude`, `OccurredAt`, `Status`, `AssignedTeam`, `Resolution`. Types: `SafetyIncident`, `VehicleIncident`, `RideIncident`, `PaymentIncident`, `TechnicalIncident`, `SecurityIncident`, `FraudIncident`, `OperationalIncident`. Severity: `Minor`/`Moderate`/`Major`/`Critical`. Statuses: `Reported`, `Acknowledged`, `Investigating`, `Resolved`, `Closed`, `Reopened`, `FalsePositive`. A Ticket may escalate into an Incident (e.g. payment ticket → discovered fraud → `FraudIncident` assigned to SecurityOfficer).

**Complaints**: about Customer/Driver/TaxiOwner/Vehicle/Garage/Roadside Partner/Advertiser/Campaign/Ride/Payment/Support interaction — evidence, responses, decisions, appeal history.

**Events**: `NotificationCreated`, `NotificationSent`, `NotificationFailed`, `SupportTicketCreated`, `SupportTicketAssigned`, `SupportTicketResponded`, `SupportTicketResolved`, `SupportTicketReopened`, `IncidentCreated`, `IncidentAcknowledged`, `IncidentEscalated`, `IncidentResolved`, `ComplaintSubmitted`, `ComplaintResolved`.

---

## Module 11 — Administration, Audit, Configuration and Analytics

**Administrative roles & least privilege**: `SuperAdmin` (full access, critical actions need enhanced security+audit), `PlatformAdmin` (general, not unrestricted), `SupportAgent`, `FinanceManager`, `AdvertiserAdmin`, `FleetManager`, `OperationsManager`, `ContentModerator`, `SecurityOfficer` — each scoped to only what their role needs (e.g. SupportAgent can't touch commissions; FinanceManager can't approve driver docs without an explicit extra permission; ContentModerator can't refund; FleetManager can't see confidential security info; SecurityOfficer can't edit invoices).

**Sensitive actions** require password/2FA confirmation, mandatory reason, authorization check, audit entry (e.g. admin suspension, large refund, commission/tax modification, security policy change, sensitive-data export, forced incident closure, admin role change).

**Administrator management**: only `SuperAdmin` does unrestricted admin-account administration (create, assign sensitive roles, suspend/reactivate, reset 2FA, revoke sessions, read full admin history).

**Role-specific dashboards** — SuperAdmin (TotalUsers, ActiveCustomers/Drivers/Vehicles/Rides, CompletedRides, CancelledRides, TotalRevenue, PlatformCommission, PendingPayments, ActiveSubscriptions, OpenSupportTickets, CriticalIncidents, PendingPartnerApprovals, ActiveCampaigns, SystemHealth), PlatformAdmin, FinanceManager, FleetManager, SecurityOfficer, ContentModerator — each with its own metric set (see full text above for exact fields).

**Audit log** (immutable): `ActorId`, `ActorRole`, `Action`, `EntityType`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `UserAgent`, `CorrelationId`, `Reason`, `CreatedAt`. Covers creation/modification/approval/rejection/suspension/refund/export/permission change/config change/sensitive access/security action. **Cannot be modified or deleted through the application**; may be archived per retention policy; export restricted to authorized roles. Sensitive read-access itself must be audited (identity docs, confidential invoices, security incidents, sensitive conversations, detailed financial data, user exports).

**Business history**: readable change history for subscription prices, commissions, contracts, vehicle ownership, driver status, campaigns, configuration.

**Security policies** (configurable): password requirements, account lockout, failed-login threshold, password history, admin password expiration, 2FA requirements, session duration, refresh-token expiration, unusual-login detection (`Allow`/`Require2FA`/`BlockTemporarily`/`FlagForReview`).

**API abuse protection**: rate limiting for Authentication/OTP/RideSearch/Payment/PublicAPI/Administration — by IP, by user, by endpoint, temporary blocking, abuse logging.

**Sensitive data**: protected in transit/at rest, masked in logs, hidden from unauthorized responses, role-restricted access. Examples: PhoneNumber, NationalId, BankAccount, TaxIdentifier, PaymentReference, DocumentUrl.

**Data retention**: configurable per accounts/documents/payments/invoices/conversations/GPS/audit logs/campaigns/incidents. Actions: `Archive`, `Anonymize`, `Delete`. Financial/audit records stay protected for their required retention period.

**Personal data requests**: access, correction, export, anonymization, account deletion — some data may be retained for financial/legal/security obligations.

**System settings**: centralized `SystemSetting` entity — `Key`, `Value`, `ValueType`, `Category`, `Description`, `IsSensitive`, `IsEditable`, `UpdatedBy`, `UpdatedAt`. Categories: General, Security, Ride, Fleet, Payment, Subscription, Loyalty, Maintenance, RoadsideAssistance, Advertising, Notification, Support, Storage. Examples: `Ride.DriverResponseTimeoutSeconds`, `Ride.MaximumSearchRadiusKm`, `Loyalty.PointExpirationMonths`, `Payment.MinimumPayoutAmount`, `Security.MaximumLoginAttempts`, `Support.ReopenDelayDays`.

**Secrets**: never store secrets (JWT secrets, payment credentials, SMTP passwords, storage credentials, external API keys) in `SystemSetting` values exposed via API — environment-based config or a future secret manager only. **No DevSecOps secret infrastructure to be implemented** (consistent with the standing DevSecOps exclusion).

**Configuration history**: critical setting changes log `OldValue`/`NewValue`/`ChangedBy`/`ChangedAt`/`Reason`; support safe rollback where possible.

**Feature flags**: e.g. `SharedRideEnabled`, `NegotiatedFareEnabled`, `AdvertisingEnabled`, `LoyaltyChallengesEnabled`, `RoadsideRecommendationsEnabled`, `MaintenanceRecommendationsEnabled` — controlled activation without touching business data.

**Geography**: `Country`/`Governorate`/`City`/`Zone`. Admin configures active cities, available services, local pricing, operating hours, search radiuses, supported vehicle categories. A `Zone` defines `RideEnabled`, `GarageEnabled`, `RoadsideAssistanceEnabled`, `AdvertisingEnabled`, `SupportedVehicleCategories`, `BaseFareRules`, `OperatingHours`.

**Configurable references**: vehicle categories, maintenance types, garage services, roadside service types, complaint categories, cancellation reasons, suspension reasons, document types, advertising categories.

**Document configuration**: `RequiredForRole`, `RequiredForVehicleCategory`, `ValidityRequired`, `MaximumFileSize`, `AllowedExtensions`, `RequiresManualReview`. Expiration reminders at configurable offsets (e.g. 60/30/7 days before, on/after expiration).

**Analytics** (aggregated): Global (UserGrowth, RideGrowth, RevenueGrowth, SubscriptionGrowth, PartnerGrowth, VehicleGrowth, CampaignGrowth, SupportTicketGrowth, IncidentGrowth, each vs. previous period + % change), Ride, Geographical, User, Driver, Maintenance, Roadside — see full metric lists above.

**Scheduled reports**: Daily/Weekly/Monthly/Quarterly; categories Financial/Operational/Security/Ride/Partner/Advertising/Support; delivered in-app or via email abstraction.

**Exports**: PDF/Excel/CSV, each with generated-by, generation date, filters, record count, export identifier, confidentiality notice.

**Health information**: exposed via backend endpoints only, for Database/Cache/FileStorage/EmailService/SmsService/PaymentGateway/BackgroundJobs/SignalR, status `Healthy`/`Degraded`/`Unhealthy`. **No external monitoring infrastructure** (consistent with the DevSecOps exclusion).

**Maintenance mode**: SuperAdmin-activatable, global/app-specific/feature-specific, custom message, admin bypass list.

**Backup preparation**: document requirements only — **no automated backup pipelines** (belongs to the future DevSecOps phase).

---

## Cross-module business events (coordination examples)

- `RideCompleted` → Payment processing → Revenue distribution → Loyalty attribution → Notification → Analytics update
- `VehicleDocumentExpired` → Vehicle suspension → Driver notification → Owner notification → Fleet dashboard alert
- `PaymentRefunded` → Ledger reversal → Invoice update → User notification → Financial report update
- `SOSActivated` → Incident creation → Security notification → Ride evidence preservation
- `TicketEscalated` → Incident creation → Security/Operations assignment
- `CampaignCompleted` → Final analytics → Revenue settlement → Partner payout eligibility

Avoid direct circular module dependencies — coordinate via events.

---

## Business implementation rules (apply to every module)

For each module, identify and implement: Aggregate Roots, Entities, Value Objects, Enumerations, Domain Events, Domain Services, Business Rules, Commands, Queries, Handlers, Validators, DTOs, Repositories, Persistence configurations, Permissions, API endpoints, Unit tests, Integration tests, Documentation.

**Do not generate every possible CRUD endpoint.** Expose only meaningful business operations:
- Prefer `POST /api/v1/rides/{rideId}/accept` over an arbitrary status update.
- Prefer `POST /api/v1/payments/{paymentId}/refund` over direct payment mutation.
- Prefer `POST /api/v1/vehicles/{vehicleId}/assign-driver` over direct relationship modification.

## Implementation priority

1. Shared foundations
2. Identity
3. Fleet
4. Ride
5. Payment and Finance
6. Subscription
7. Notification
8. Maintenance
9. Roadside Assistance
10. Loyalty
11. Advertising
12. Support and Incidents
13. Administration and Analytics

**The solution must compile after every phase.** Do not start a new major phase before summarizing the previous phase and receiving approval when required.
