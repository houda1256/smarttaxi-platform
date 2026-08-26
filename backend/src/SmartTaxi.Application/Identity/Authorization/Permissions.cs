namespace SmartTaxi.Application.Identity.Authorization;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";

    public const string DocumentsReadOwn = "documents.read.own";
    public const string DocumentsUploadOwn = "documents.upload.own";
    public const string DocumentsReadAll = "documents.read.all";
    public const string DocumentsReview = "documents.review";
    public const string DocumentsSuspend = "documents.suspend";
    public const string DocumentsConfigure = "documents.configure";

    public const string ProfessionalRegisterOwn = "professional.register.own";
    public const string ProfessionalReview = "professional.review";
    public const string ReferralsManage = "referrals.manage";
    public const string PreferencesManageOwn = "preferences.manage.own";
    public const string DataRequestsSubmitOwn = "data-requests.submit.own";
    public const string DataRequestsProcess = "data-requests.process";

    public const string FleetOwnerProfileManageOwn = "fleet.owner-profile.manage.own";
    public const string FleetManageOwn = "fleet.manage.own";
    public const string FleetVehiclesManageOwn = "fleet.vehicles.manage.own";
    public const string FleetVehiclesReview = "fleet.vehicles.review";
    public const string FleetVehicleDocumentsManageOwn = "fleet.vehicle-documents.manage.own";
    public const string FleetVehicleDocumentsReview = "fleet.vehicle-documents.review";
    public const string FleetVehicleDocumentsReadAll = "fleet.vehicle-documents.read.all";
    public const string FleetDriversManageOwn = "fleet.drivers.manage.own";
    public const string FleetDriversReview = "fleet.drivers.review";
    public const string FleetAssignmentsManageOwn = "fleet.assignments.manage.own";
    public const string FleetContractsManageOwn = "fleet.contracts.manage.own";
    public const string FleetContractsReadOwn = "fleet.contracts.read.own";
    public const string FleetExpensesManageOwn = "fleet.expenses.manage.own";
    public const string FleetAlertsManageOwn = "fleet.alerts.manage.own";
    public const string FleetUsageReadOwn = "fleet.usage.read.own";

    public const string RidesCreate = "rides.create";
    public const string RidesReadOwn = "rides.read.own";
    public const string RidesSearchDrivers = "rides.search-drivers";
    public const string RidesSelectDriver = "rides.select-driver";
    public const string RidesAccept = "rides.accept";
    public const string RidesReject = "rides.reject";
    public const string RidesUpdateLocation = "rides.update-location";
    public const string RidesStart = "rides.start";
    public const string RidesComplete = "rides.complete";
    public const string RidesCancelOwn = "rides.cancel.own";
    public const string RidesCancelAdmin = "rides.cancel.admin";
    public const string RidesMonitor = "rides.monitor";
    public const string RidesDispute = "rides.dispute";
    public const string RidesRate = "rides.rate";
    public const string RidesSos = "rides.sos";
    public const string RidesSharedManage = "rides.shared.manage";
    public const string RidesPricingManage = "rides.pricing.manage";

    public const string PaymentsCreate = "payments.create";
    public const string PaymentsReadOwn = "payments.read.own";
    public const string PaymentsConfirm = "payments.confirm";
    public const string PaymentsCancel = "payments.cancel";
    public const string PaymentsRefund = "payments.refund";
    public const string PaymentsReport = "payments.report";
    public const string PaymentsManage = "payments.manage";

    public const string FinanceAccountsReadOwn = "finance.accounts.read.own";
    public const string FinanceAccountsReadAll = "finance.accounts.read.all";
    public const string FinanceLedgerRead = "finance.ledger.read";
    public const string FinancePayoutsRequestOwn = "finance.payouts.request.own";
    public const string FinancePayoutsReadOwn = "finance.payouts.read.own";
    public const string FinancePayoutsManage = "finance.payouts.manage";
    public const string FinanceCashRegisterManageOwn = "finance.cash-register.manage.own";
    public const string FinanceCashRegisterAudit = "finance.cash-register.audit";
    public const string FinanceCashDeclarationsSubmitOwn = "finance.cash-declarations.submit.own";
    public const string FinanceCashDeclarationsReview = "finance.cash-declarations.review";
    public const string FinanceBusinessCustomersManage = "finance.business-customers.manage";
    public const string FinanceBusinessCustomersReadOwn = "finance.business-customers.read.own";
    public const string FinanceTaxManage = "finance.tax.manage";
    public const string FinanceDisputesOpenOwn = "finance.disputes.open.own";
    public const string FinanceDisputesManage = "finance.disputes.manage";
    public const string FinanceReportsRead = "finance.reports.read";

    public const string SubscriptionPlansRead = "subscription.plans.read";
    public const string SubscriptionPlansManage = "subscription.plans.manage";
    public const string SubscriptionReadOwn = "subscription.read.own";
    public const string SubscriptionCreate = "subscription.create";
    public const string SubscriptionRenewOwn = "subscription.renew.own";
    public const string SubscriptionCancelOwn = "subscription.cancel.own";
    public const string SubscriptionManage = "subscription.manage";

    public const string NotificationsReadOwn = "notifications.read.own";
    public const string NotificationsPreferencesManageOwn = "notifications.preferences.manage.own";
    public const string NotificationsDeviceTokensManageOwn = "notifications.device-tokens.manage.own";
    public const string NotificationsTemplatesManage = "notifications.templates.manage";
    public const string NotificationsDeliveryRead = "notifications.delivery.read";
    public const string NotificationsDeliveryManage = "notifications.delivery.manage";

    public const string LoyaltyAccountReadOwn = "loyalty.account.read.own";
    public const string LoyaltyRewardsRead = "loyalty.rewards.read";
    public const string LoyaltyRedemptionCreateOwn = "loyalty.redemption.create.own";
    public const string LoyaltyChallengesRead = "loyalty.challenges.read";
    public const string LoyaltyRulesManage = "loyalty.rules.manage";
    public const string LoyaltyCatalogManage = "loyalty.catalog.manage";
    public const string LoyaltyAdjustmentsManage = "loyalty.adjustments.manage";

    public const string AdvertisingProfileManageOwn = "advertising.profile.manage.own";
    public const string AdvertisingCampaignsReadOwn = "advertising.campaigns.read.own";
    public const string AdvertisingCampaignsManageOwn = "advertising.campaigns.manage.own";
    public const string AdvertisingCampaignsSubmitOwn = "advertising.campaigns.submit.own";
    public const string AdvertisingPerformanceReadOwn = "advertising.performance.read.own";

    /// <summary>Granted to Customer/Driver, never Advertiser/Admin — this is the consumer-side app (rider/driver) actually viewing/clicking a served ad, not the advertiser managing the campaign.</summary>
    public const string AdvertisingTrackingRecord = "advertising.tracking.record";
    public const string AdvertisingCampaignsReview = "advertising.campaigns.review";
    public const string AdvertisingCampaignsReadAll = "advertising.campaigns.read.all";
    public const string AdvertisingPlacementsManage = "advertising.placements.manage";
    public const string AdvertisingPerformanceReadAll = "advertising.performance.read.all";

    public const string MaintenanceGarageProfileManageOwn = "maintenance.garage-profile.manage.own";
    public const string MaintenanceRequestsCreateOwn = "maintenance.requests.create.own";
    public const string MaintenanceRequestsReadOwn = "maintenance.requests.read.own";
    public const string MaintenanceRequestsManageOwn = "maintenance.requests.manage.own";
    public const string MaintenanceJobsManageOwn = "maintenance.jobs.manage.own";
    public const string MaintenanceRecordsReadOwn = "maintenance.records.read.own";
    public const string MaintenanceReadAll = "maintenance.read.all";
    public const string MaintenanceManageAll = "maintenance.manage.all";

    public const string RoadsidePartnerProfileManageOwn = "roadside.partner-profile.manage.own";
    public const string RoadsideRequestsCreateOwn = "roadside.requests.create.own";
    public const string RoadsideRequestsReadOwn = "roadside.requests.read.own";
    public const string RoadsideRequestsManageOwn = "roadside.requests.manage.own";
    public const string RoadsideJobsManageOwn = "roadside.jobs.manage.own";
    public const string RoadsideReadAll = "roadside.read.all";
    public const string RoadsideManageAll = "roadside.manage.all";

    public static readonly IReadOnlyCollection<string> All =
    [
        UsersRead,
        UsersManage,
        DocumentsReadOwn,
        DocumentsUploadOwn,
        DocumentsReadAll,
        DocumentsReview,
        DocumentsSuspend,
        DocumentsConfigure,
        ProfessionalRegisterOwn,
        ProfessionalReview,
        ReferralsManage,
        PreferencesManageOwn,
        DataRequestsSubmitOwn,
        DataRequestsProcess,
        FleetOwnerProfileManageOwn,
        FleetManageOwn,
        FleetVehiclesManageOwn,
        FleetVehiclesReview,
        FleetVehicleDocumentsManageOwn,
        FleetVehicleDocumentsReview,
        FleetVehicleDocumentsReadAll,
        FleetDriversManageOwn,
        FleetDriversReview,
        FleetAssignmentsManageOwn,
        FleetContractsManageOwn,
        FleetContractsReadOwn,
        FleetExpensesManageOwn,
        FleetAlertsManageOwn,
        FleetUsageReadOwn,
        RidesCreate,
        RidesReadOwn,
        RidesSearchDrivers,
        RidesSelectDriver,
        RidesAccept,
        RidesReject,
        RidesUpdateLocation,
        RidesStart,
        RidesComplete,
        RidesCancelOwn,
        RidesCancelAdmin,
        RidesMonitor,
        RidesDispute,
        RidesRate,
        RidesSos,
        RidesSharedManage,
        RidesPricingManage,
        PaymentsCreate,
        PaymentsReadOwn,
        PaymentsConfirm,
        PaymentsCancel,
        PaymentsRefund,
        PaymentsReport,
        PaymentsManage,
        FinanceAccountsReadOwn,
        FinanceAccountsReadAll,
        FinanceLedgerRead,
        FinancePayoutsRequestOwn,
        FinancePayoutsReadOwn,
        FinancePayoutsManage,
        FinanceCashRegisterManageOwn,
        FinanceCashRegisterAudit,
        FinanceCashDeclarationsSubmitOwn,
        FinanceCashDeclarationsReview,
        FinanceBusinessCustomersManage,
        FinanceBusinessCustomersReadOwn,
        FinanceTaxManage,
        FinanceDisputesOpenOwn,
        FinanceDisputesManage,
        FinanceReportsRead,
        SubscriptionPlansRead,
        SubscriptionPlansManage,
        SubscriptionReadOwn,
        SubscriptionCreate,
        SubscriptionRenewOwn,
        SubscriptionCancelOwn,
        SubscriptionManage,
        NotificationsReadOwn,
        NotificationsPreferencesManageOwn,
        NotificationsDeviceTokensManageOwn,
        NotificationsTemplatesManage,
        NotificationsDeliveryRead,
        NotificationsDeliveryManage,
        LoyaltyAccountReadOwn,
        LoyaltyRewardsRead,
        LoyaltyRedemptionCreateOwn,
        LoyaltyChallengesRead,
        LoyaltyRulesManage,
        LoyaltyCatalogManage,
        LoyaltyAdjustmentsManage,
        AdvertisingProfileManageOwn,
        AdvertisingCampaignsReadOwn,
        AdvertisingCampaignsManageOwn,
        AdvertisingCampaignsSubmitOwn,
        AdvertisingPerformanceReadOwn,
        AdvertisingTrackingRecord,
        AdvertisingCampaignsReview,
        AdvertisingCampaignsReadAll,
        AdvertisingPlacementsManage,
        AdvertisingPerformanceReadAll,
        MaintenanceGarageProfileManageOwn,
        MaintenanceRequestsCreateOwn,
        MaintenanceRequestsReadOwn,
        MaintenanceRequestsManageOwn,
        MaintenanceJobsManageOwn,
        MaintenanceRecordsReadOwn,
        MaintenanceReadAll,
        MaintenanceManageAll,
        RoadsidePartnerProfileManageOwn,
        RoadsideRequestsCreateOwn,
        RoadsideRequestsReadOwn,
        RoadsideRequestsManageOwn,
        RoadsideJobsManageOwn,
        RoadsideReadAll,
        RoadsideManageAll
    ];
}
