using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Application.Fleet.Alerts.Abstractions;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Application.Fleet.Owners.Abstractions;
using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.DataRequests;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Application.Identity.Professional.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Payments;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Application.Payments.Ledger;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Application.Payments.Reports.Abstractions;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Application.Rides;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Identity.Options;
using SmartTaxi.Infrastructure.Identity.Repositories;
using SmartTaxi.Infrastructure.Identity.Services;
using SmartTaxi.Infrastructure.Payments.Options;
using SmartTaxi.Infrastructure.Payments.Policies;
using SmartTaxi.Infrastructure.Payments.Repositories;
using SmartTaxi.Infrastructure.Payments.Services;
using SmartTaxi.Infrastructure.Persistence;
using SmartTaxi.Infrastructure.Rides.Options;
using SmartTaxi.Infrastructure.Rides.Policies;
using SmartTaxi.Infrastructure.Rides.Repositories;
using SmartTaxi.Infrastructure.Rides.Services;

namespace SmartTaxi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<IPhoneVerificationOtpRepository, PhoneVerificationOtpRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ITwoFactorChallengeRepository, TwoFactorChallengeRepository>();
        services.AddScoped<ITwoFactorRecoveryCodeRepository, TwoFactorRecoveryCodeRepository>();
        services.AddScoped<RecoveryCodeService>();
        services.AddScoped<IUserDocumentRepository, UserDocumentRepository>();
        services.AddScoped<IDocumentAccessAuditRepository, DocumentAccessAuditRepository>();
        services.AddScoped<DocumentUploadValidator>();
        services.AddScoped<DocumentEligibilityChecker>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Scoped (not Singleton): JwtTokenGenerator now depends on
        // IRolePermissionRepository, which uses the scoped ApplicationDbContext.
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.AddSingleton<IRefreshTokenPolicy, RefreshTokenPolicy>();

        services.Configure<EmailVerificationOptions>(configuration.GetSection(EmailVerificationOptions.SectionName));
        services.Configure<PhoneVerificationOptions>(configuration.GetSection(PhoneVerificationOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));
        services.Configure<TwoFactorOptions>(configuration.GetSection(TwoFactorOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));

        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<ISmsSender, LoggingSmsSender>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();
        services.AddSingleton<ITwoFactorSecretProtector, TwoFactorSecretProtector>();

        services.AddSingleton<IEmailVerificationPolicy, EmailVerificationPolicy>();
        services.AddSingleton<IPhoneVerificationPolicy, PhoneVerificationPolicy>();
        services.AddSingleton<IPasswordResetPolicy, PasswordResetPolicy>();
        services.AddSingleton<ITwoFactorPolicy, TwoFactorPolicy>();
        services.AddSingleton<ISecurityPolicy, SecurityPolicy>();

        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));
        services.Configure<DocumentExpirationOptions>(configuration.GetSection(DocumentExpirationOptions.SectionName));
        services.AddSingleton<IDocumentUploadPolicy, DocumentUploadPolicy>();
        services.AddSingleton<IDocumentExpirationPolicy, DocumentExpirationPolicy>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<IProfessionalAccountRequestRepository, ProfessionalAccountRequestRepository>();

        services.AddScoped<IReferralRepository, ReferralRepository>();
        services.AddSingleton<IReferralCodeGenerator, ReferralCodeGenerator>();
        services.Configure<ReferralActivationOptions>(configuration.GetSection(ReferralActivationOptions.SectionName));
        services.AddSingleton<IReferralActivationPolicy, ReferralActivationPolicy>();

        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        services.AddScoped<IPersonalDataRequestRepository, PersonalDataRequestRepository>();
        services.AddScoped<PersonalDataExportBuilder>();

        services.AddScoped<ITaxiOwnerProfileRepository, TaxiOwnerProfileRepository>();
        services.AddScoped<IFleetRepository, FleetRepository>();
        services.AddScoped<IFleetMemberRepository, FleetMemberRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleDocumentRepository, VehicleDocumentRepository>();
        services.AddScoped<IVehicleDocumentAccessAuditRepository, VehicleDocumentAccessAuditRepository>();
        services.AddScoped<VehicleEligibilityChecker>();
        services.AddScoped<IDriverProfileRepository, DriverProfileRepository>();
        services.AddScoped<IDriverVehicleAssignmentRepository, DriverVehicleAssignmentRepository>();
        services.AddScoped<IDriverOwnerContractRepository, DriverOwnerContractRepository>();
        services.AddScoped<IFleetExpenseRepository, FleetExpenseRepository>();
        services.AddScoped<IVehicleUsageRecordRepository, VehicleUsageRecordRepository>();
        services.AddScoped<IFleetAlertRepository, FleetAlertRepository>();

        services.AddScoped<IRideRepository, RideRepository>();
        services.AddScoped<IRideStatusHistoryRepository, RideStatusHistoryRepository>();
        services.AddScoped<IDriverReservationHoldRepository, DriverReservationHoldRepository>();
        services.AddScoped<IRideDriverRecommendationRepository, RideDriverRecommendationRepository>();
        services.AddScoped<IRideFareProposalRepository, RideFareProposalRepository>();
        services.AddScoped<ISharedRideMatchRepository, SharedRideMatchRepository>();
        services.AddScoped<ISharedRideParticipantRepository, SharedRideParticipantRepository>();
        services.AddScoped<IRideLocationPointRepository, RideLocationPointRepository>();
        services.AddScoped<IRideShareTokenRepository, RideShareTokenRepository>();
        services.AddScoped<IRideRatingRepository, RideRatingRepository>();
        services.AddScoped<IRideSafetyEventRepository, RideSafetyEventRepository>();
        services.AddScoped<IRideComplaintRepository, RideComplaintRepository>();
        services.AddScoped<IRideConversationRepository, RideConversationRepository>();
        services.AddScoped<IRideMessageRepository, RideMessageRepository>();

        services.Configure<RidePricingOptions>(configuration.GetSection(RidePricingOptions.SectionName));
        services.Configure<RideDriverSearchOptions>(configuration.GetSection(RideDriverSearchOptions.SectionName));
        services.Configure<RideNegotiationOptions>(configuration.GetSection(RideNegotiationOptions.SectionName));
        services.Configure<SharedRideMatchingOptions>(configuration.GetSection(SharedRideMatchingOptions.SectionName));
        services.Configure<RideServiceOptions>(configuration.GetSection(RideServiceOptions.SectionName));

        services.AddSingleton<IFarePricingPolicy, RideFarePricingPolicy>();
        services.AddSingleton<IDriverSearchPolicy, RideDriverSearchPolicy>();
        services.AddSingleton<INegotiationPolicy, RideNegotiationPolicy>();
        services.AddSingleton<ISharedRideMatchingPolicy, RideSharedRideMatchingPolicy>();
        services.AddSingleton<IRideServicePolicy, RideServicePolicy>();

        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
        services.AddSingleton<IRouteEstimationService, RouteEstimationService>();
        services.AddSingleton<IFareCalculator, RideFareCalculator>();
        services.AddSingleton<IDynamicPricingProvider, RideDynamicPricingProvider>();
        services.AddSingleton<ISharedRideMatchingService, RideSharedRideMatchingService>();
        services.AddSingleton<IRideShareTokenGenerator, RideShareTokenGenerator>();
        services.AddSingleton<IRideShareTokenHasher, RideShareTokenHasher>();

        services.AddScoped<IDriverRecommendationService, DriverRecommendationService>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentTransactionHistoryRepository, PaymentTransactionHistoryRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IRefundRecordRepository, RefundRecordRepository>();

        services.Configure<PlatformCommissionOptions>(configuration.GetSection(PlatformCommissionOptions.SectionName));
        services.Configure<InvoiceTaxOptions>(configuration.GetSection(InvoiceTaxOptions.SectionName));

        services.AddSingleton<IPlatformCommissionPolicy, PlatformCommissionPolicy>();
        services.AddSingleton<IInvoiceTaxPolicy, InvoiceTaxPolicy>();

        services.AddSingleton<IInvoicePdfGenerator, DevInvoicePdfGenerator>();
        services.AddSingleton<IReceiptPdfGenerator, DevReceiptPdfGenerator>();

        services.AddScoped<IRevenueSharingCalculator, RevenueSharingCalculator>();

        services.AddScoped<IFinancialAccountRepository, FinancialAccountRepository>();
        services.AddScoped<IFinancialLedgerRepository, FinancialLedgerRepository>();
        services.AddScoped<ILedgerPostingService, LedgerPostingService>();

        services.AddScoped<IPayoutRepository, PayoutRepository>();
        services.Configure<PayoutOptions>(configuration.GetSection(PayoutOptions.SectionName));
        services.AddSingleton<IPayoutPolicy, PayoutPolicy>();

        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<ICashRegisterSessionRepository, CashRegisterSessionRepository>();
        services.AddScoped<ICashMovementRepository, CashMovementRepository>();

        services.AddScoped<ICashDeclarationRepository, CashDeclarationRepository>();

        services.AddScoped<IBusinessCustomerRepository, BusinessCustomerRepository>();
        services.AddScoped<IBusinessCustomerEmployeeRepository, BusinessCustomerEmployeeRepository>();

        services.AddScoped<ITaxRuleRepository, TaxRuleRepository>();

        services.AddScoped<IGroupedInvoiceRepository, GroupedInvoiceRepository>();
        services.AddScoped<IGroupedInvoiceLineRepository, GroupedInvoiceLineRepository>();

        services.AddScoped<IFinancialDisputeRepository, FinancialDisputeRepository>();

        services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
        services.AddSingleton<IReportExporter, DevReportExporter>();

        return services;
    }
}
