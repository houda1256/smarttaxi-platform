using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Owners.Entities;
using SmartTaxi.Domain.Fleet.UsageHistory.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Preferences.Entities;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.Ledger.Entities;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;
using SmartTaxi.Domain.Payments.Taxes.Entities;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Subscriptions.Entities;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Infrastructure.Persistence.Entities;

namespace SmartTaxi.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<UserSession> Sessions => Set<UserSession>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<PhoneVerificationOtp> PhoneVerificationOtps => Set<PhoneVerificationOtp>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();

    public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => Set<TwoFactorRecoveryCode>();

    public DbSet<UserDocument> UserDocuments => Set<UserDocument>();

    public DbSet<DocumentAccessAuditEntry> DocumentAccessAuditEntries => Set<DocumentAccessAuditEntry>();

    public DbSet<ProfessionalAccountRequest> ProfessionalAccountRequests => Set<ProfessionalAccountRequest>();

    public DbSet<Referral> Referrals => Set<Referral>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    public DbSet<PersonalDataRequest> PersonalDataRequests => Set<PersonalDataRequest>();

    public DbSet<TaxiOwnerProfile> TaxiOwnerProfiles => Set<TaxiOwnerProfile>();

    public DbSet<FleetOrganization> Fleets => Set<FleetOrganization>();

    public DbSet<FleetMember> FleetMembers => Set<FleetMember>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();

    public DbSet<VehicleDocumentAccessAuditEntry> VehicleDocumentAccessAuditEntries => Set<VehicleDocumentAccessAuditEntry>();

    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();

    public DbSet<DriverVehicleAssignment> DriverVehicleAssignments => Set<DriverVehicleAssignment>();

    public DbSet<DriverOwnerContract> DriverOwnerContracts => Set<DriverOwnerContract>();

    public DbSet<FleetExpense> FleetExpenses => Set<FleetExpense>();

    public DbSet<VehicleUsageRecord> VehicleUsageRecords => Set<VehicleUsageRecord>();

    public DbSet<FleetAlert> FleetAlerts => Set<FleetAlert>();

    public DbSet<Ride> Rides => Set<Ride>();

    public DbSet<RideStatusHistory> RideStatusHistories => Set<RideStatusHistory>();

    public DbSet<DriverReservationHold> DriverReservationHolds => Set<DriverReservationHold>();

    public DbSet<RideDriverRecommendation> RideDriverRecommendations => Set<RideDriverRecommendation>();

    public DbSet<RideFareProposal> RideFareProposals => Set<RideFareProposal>();

    public DbSet<SharedRideMatch> SharedRideMatches => Set<SharedRideMatch>();

    public DbSet<SharedRideParticipant> SharedRideParticipants => Set<SharedRideParticipant>();

    public DbSet<RideLocationPoint> RideLocationPoints => Set<RideLocationPoint>();

    public DbSet<RideShareToken> RideShareTokens => Set<RideShareToken>();

    public DbSet<RideRating> RideRatings => Set<RideRating>();

    public DbSet<RideSafetyEvent> RideSafetyEvents => Set<RideSafetyEvent>();

    public DbSet<RideComplaint> RideComplaints => Set<RideComplaint>();

    public DbSet<RideConversation> RideConversations => Set<RideConversation>();

    public DbSet<RideMessage> RideMessages => Set<RideMessage>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentTransactionHistory> PaymentTransactionHistories => Set<PaymentTransactionHistory>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Receipt> Receipts => Set<Receipt>();

    public DbSet<RefundRecord> RefundRecords => Set<RefundRecord>();

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    public DbSet<FinancialLedgerEntry> FinancialLedgerEntries => Set<FinancialLedgerEntry>();

    public DbSet<Payout> Payouts => Set<Payout>();

    public DbSet<CashRegisterBox> CashRegisters => Set<CashRegisterBox>();

    public DbSet<CashRegisterSession> CashRegisterSessions => Set<CashRegisterSession>();

    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    public DbSet<CashDeclaration> CashDeclarations => Set<CashDeclaration>();

    public DbSet<BusinessCustomer> BusinessCustomers => Set<BusinessCustomer>();

    public DbSet<BusinessCustomerEmployee> BusinessCustomerEmployees => Set<BusinessCustomerEmployee>();

    public DbSet<TaxRule> TaxRules => Set<TaxRule>();

    public DbSet<SubscriptionCharge> SubscriptionCharges => Set<SubscriptionCharge>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<GroupedInvoice> GroupedInvoices => Set<GroupedInvoice>();

    public DbSet<GroupedInvoiceLine> GroupedInvoiceLines => Set<GroupedInvoiceLine>();

    public DbSet<FinancialDispute> FinancialDisputes => Set<FinancialDispute>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<ScheduledNotification> ScheduledNotifications => Set<ScheduledNotification>();

    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();

    public DbSet<LoyaltyPointLedgerEntry> LoyaltyPointLedgerEntries => Set<LoyaltyPointLedgerEntry>();

    public DbSet<LoyaltyEarningRule> LoyaltyEarningRules => Set<LoyaltyEarningRule>();

    public DbSet<LoyaltyTierThreshold> LoyaltyTierThresholds => Set<LoyaltyTierThreshold>();

    public DbSet<LoyaltyReward> LoyaltyRewards => Set<LoyaltyReward>();

    public DbSet<LoyaltyRedemption> LoyaltyRedemptions => Set<LoyaltyRedemption>();

    public DbSet<LoyaltyReferralReward> LoyaltyReferralRewards => Set<LoyaltyReferralReward>();

    public DbSet<LoyaltyChallenge> LoyaltyChallenges => Set<LoyaltyChallenge>();

    public DbSet<LoyaltyChallengeProgress> LoyaltyChallengeProgresses => Set<LoyaltyChallengeProgress>();

    public DbSet<AdvertiserProfile> AdvertiserProfiles => Set<AdvertiserProfile>();

    public DbSet<AdvertisingPlacement> AdvertisingPlacements => Set<AdvertisingPlacement>();

    public DbSet<AdCampaign> AdCampaigns => Set<AdCampaign>();

    public DbSet<CampaignCreative> CampaignCreatives => Set<CampaignCreative>();

    public DbSet<AdCampaignReviewHistoryEntry> AdCampaignReviewHistoryEntries => Set<AdCampaignReviewHistoryEntry>();

    public DbSet<AdvertisingImpression> AdvertisingImpressions => Set<AdvertisingImpression>();

    public DbSet<AdvertisingClick> AdvertisingClicks => Set<AdvertisingClick>();

    public DbSet<GarageProfile> GarageProfiles => Set<GarageProfile>();

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    public DbSet<RoadsidePartnerProfile> RoadsidePartnerProfiles => Set<RoadsidePartnerProfile>();

    public DbSet<RoadsideAssistanceRequest> RoadsideAssistanceRequests => Set<RoadsideAssistanceRequest>();

    public DbSet<RoadsidePartnerSelectionHistory> RoadsidePartnerSelectionHistories => Set<RoadsidePartnerSelectionHistory>();

    internal DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();

    public DbSet<SupportIncident> SupportIncidents => Set<SupportIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
