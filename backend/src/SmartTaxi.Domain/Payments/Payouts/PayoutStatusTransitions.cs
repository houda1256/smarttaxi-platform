using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Domain.Payments.Payouts;

/// <summary>Mirrors RideStatusTransitions/PaymentStatusTransitions — the single source of truth for legal Payout status transitions.</summary>
public static class PayoutStatusTransitions
{
    private static readonly PayoutStatus[] TerminalStatuses =
    [
        PayoutStatus.Paid, PayoutStatus.Failed, PayoutStatus.Rejected, PayoutStatus.Cancelled
    ];

    private static readonly Dictionary<PayoutStatus, PayoutStatus[]> Allowed = new()
    {
        [PayoutStatus.Requested] = [PayoutStatus.PendingApproval, PayoutStatus.Cancelled],
        [PayoutStatus.PendingApproval] = [PayoutStatus.Approved, PayoutStatus.Rejected, PayoutStatus.Cancelled],
        [PayoutStatus.Approved] = [PayoutStatus.Processing, PayoutStatus.Cancelled],
        [PayoutStatus.Processing] = [PayoutStatus.Paid, PayoutStatus.Failed],
        [PayoutStatus.Paid] = [],
        [PayoutStatus.Failed] = [],
        [PayoutStatus.Rejected] = [],
        [PayoutStatus.Cancelled] = []
    };

    public static bool CanTransition(PayoutStatus from, PayoutStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(PayoutStatus status) => TerminalStatuses.Contains(status);
}
