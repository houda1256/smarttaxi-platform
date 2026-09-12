using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Reports;
using SmartTaxi.Application.Payments.Reports.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Enums;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

/// <summary>
/// Every figure is a real aggregation over existing tables — nothing is
/// fabricated or stored redundantly. Two documented simplifications:
/// (1) NetProfit = PlatformCommission - Expenses (retained commission minus
/// operating expenses) — the platform's own share only, not a full P&amp;L.
/// (2) The "Service" filter is accepted (the master prompt requires it in
/// the filter set) but currently a no-op: no transactional row in this
/// schema carries a service tag to filter by (TaxRule.ApplicableService
/// describes a rule, not a transaction) — flagged here rather than
/// pretending to filter something that doesn't exist.
/// </summary>
internal sealed class FinancialReportRepository : IFinancialReportRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialReportResult> GetReportAsync(FinancialReportFilter filter, CancellationToken cancellationToken)
    {
        var payments = _context.Payments.Where(p => p.ConfirmedAt != null && p.ConfirmedAt >= filter.FromUtc && p.ConfirmedAt <= filter.ToUtc);

        if (filter.ActorId is { } paymentActorId)
        {
            payments = payments.Where(p => p.CustomerId == paymentActorId || p.DriverId == paymentActorId || p.OwnerId == paymentActorId);
        }

        if (filter.VehicleId is { } vehicleId)
        {
            payments = payments.Where(p => p.VehicleId == vehicleId);
        }

        if (Enum.TryParse<PaymentMethod>(filter.PaymentMethod, out var paymentMethodValue))
        {
            payments = payments.Where(p => p.PaymentMethod == paymentMethodValue);
        }

        if (Enum.TryParse<PaymentStatus>(filter.Status, out var paymentStatusValue))
        {
            payments = payments.Where(p => p.Status == paymentStatusValue);
        }

        var grossRevenue = await payments.SumAsync(p => (decimal?)p.FinalFareAmount, cancellationToken) ?? 0m;
        var cardPayments = await payments
            .Where(p => p.PaymentMethod == PaymentMethod.Card)
            .SumAsync(p => (decimal?)p.FinalFareAmount, cancellationToken) ?? 0m;

        var refundQuery = _context.RefundRecords.Where(r => r.ProcessedAt >= filter.FromUtc && r.ProcessedAt <= filter.ToUtc);
        var refundAmount = await refundQuery.SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var cashCollected = await _context.CashMovements
            .Where(m => m.MovementType == CashMovementType.RideIncome && m.RecordedAt >= filter.FromUtc && m.RecordedAt <= filter.ToUtc)
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0m;

        var ledgerQuery = _context.FinancialLedgerEntries.Where(e => e.CreatedAt >= filter.FromUtc && e.CreatedAt <= filter.ToUtc);
        var platformCommission = await ledgerQuery
            .Where(e => e.EntryType == LedgerEntryType.PlatformCommission)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var driverEarnings = await ledgerQuery
            .Where(e => e.EntryType == LedgerEntryType.DriverEarning)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var ownerEarnings = await ledgerQuery
            .Where(e => e.EntryType == LedgerEntryType.OwnerEarning)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var partnerEarnings = await ledgerQuery
            .Where(e => e.EntryType == LedgerEntryType.PartnerEarning)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var outstandingInvoicesQuery = _context.GroupedInvoices
            .Where(i => i.Status == GroupedInvoiceStatus.Issued || i.Status == GroupedInvoiceStatus.Overdue);

        if (filter.ActorId is { } businessCustomerId)
        {
            outstandingInvoicesQuery = outstandingInvoicesQuery.Where(i => i.BusinessCustomerId == businessCustomerId);
        }

        var outstandingInvoices = await outstandingInvoicesQuery.SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var expensesQuery = _context.FleetExpenses.Where(expense =>
            expense.Status == Domain.Fleet.Expenses.Enums.ExpenseStatus.Approved
            && expense.ExpenseDate >= DateOnly.FromDateTime(filter.FromUtc) && expense.ExpenseDate <= DateOnly.FromDateTime(filter.ToUtc));

        if (filter.ActorId is { } expenseOwnerId)
        {
            expensesQuery = expensesQuery.Where(expense => expense.OwnerId == expenseOwnerId);
        }

        if (filter.VehicleId is { } expenseVehicleId)
        {
            expensesQuery = expensesQuery.Where(expense => expense.VehicleId == expenseVehicleId);
        }

        if (filter.FleetId is { } fleetId)
        {
            expensesQuery = expensesQuery.Where(expense => expense.FleetId == fleetId);
        }

        var expenses = await expensesQuery.SumAsync(expense => (decimal?)expense.Money.Amount, cancellationToken) ?? 0m;

        var payoutsQuery =
            from payout in _context.Payouts
            join account in _context.FinancialAccounts on payout.BeneficiaryAccountId equals account.Id
            where payout.Status == Domain.Payments.Payouts.Enums.PayoutStatus.Paid && payout.PaidAt >= filter.FromUtc && payout.PaidAt <= filter.ToUtc
            select new { payout.Amount, account.OwnerReferenceId };

        if (filter.ActorId is { } payoutActorId)
        {
            payoutsQuery = payoutsQuery.Where(x => x.OwnerReferenceId == payoutActorId);
        }

        var payoutsTotal = await payoutsQuery.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var debtsQuery = _context.FinancialAccounts.AsQueryable();

        if (filter.ActorId is { } debtActorId)
        {
            debtsQuery = debtsQuery.Where(account => account.OwnerReferenceId == debtActorId);
        }

        var debtsTotal = await debtsQuery.SumAsync(account => (decimal?)account.DebtBalance, cancellationToken) ?? 0m;

        var cashDifferencesTotal = await _context.CashRegisterSessions
            .Where(session => session.ClosedAt != null && session.ClosedAt >= filter.FromUtc && session.ClosedAt <= filter.ToUtc)
            .SumAsync(session => (decimal?)(session.Difference ?? 0m), cancellationToken) ?? 0m;

        var invoiceTax = await _context.Invoices
            .Where(invoice => invoice.CreatedAt >= filter.FromUtc && invoice.CreatedAt <= filter.ToUtc)
            .SumAsync(invoice => (decimal?)invoice.TaxAmount, cancellationToken) ?? 0m;
        var groupedInvoiceTax = await _context.GroupedInvoices
            .Where(invoice => invoice.IssueDate >= filter.FromUtc && invoice.IssueDate <= filter.ToUtc)
            .SumAsync(invoice => (decimal?)invoice.TaxAmount, cancellationToken) ?? 0m;
        var taxTotal = invoiceTax + groupedInvoiceTax;

        var netRevenue = grossRevenue - refundAmount;
        var netProfit = platformCommission - expenses;

        return new FinancialReportResult(
            grossRevenue, netRevenue, platformCommission, driverEarnings, ownerEarnings, partnerEarnings, refundAmount,
            outstandingInvoices, cashCollected, cardPayments, expenses, netProfit, payoutsTotal, debtsTotal, cashDifferencesTotal, taxTotal);
    }
}
