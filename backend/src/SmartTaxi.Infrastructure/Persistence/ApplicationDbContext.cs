using Microsoft.EntityFrameworkCore;
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
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.Ledger.Entities;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Taxes.Entities;
using SmartTaxi.Domain.Rides.Entities;
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

    public DbSet<GroupedInvoice> GroupedInvoices => Set<GroupedInvoice>();

    public DbSet<GroupedInvoiceLine> GroupedInvoiceLines => Set<GroupedInvoiceLine>();

    public DbSet<FinancialDispute> FinancialDisputes => Set<FinancialDispute>();

    internal DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
