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
        FinanceReportsRead
    ];
}
