using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Domain.Payments;

/// <summary>
/// The single source of truth for which Payment status transitions are
/// legal — mirrors RideStatusTransitions. Every atomic repository-level
/// transition encodes its own specific "from" status as a WHERE guard
/// already; this map exists so the full graph is documented in one place
/// and is unit-testable.
/// </summary>
public static class PaymentStatusTransitions
{
    private static readonly PaymentStatus[] TerminalStatuses =
    [
        PaymentStatus.Failed, PaymentStatus.Cancelled, PaymentStatus.Refunded
    ];

    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> Allowed = new()
    {
        [PaymentStatus.Pending] = [PaymentStatus.Authorized, PaymentStatus.Paid, PaymentStatus.Failed, PaymentStatus.Cancelled],
        [PaymentStatus.Authorized] = [PaymentStatus.Paid, PaymentStatus.Failed, PaymentStatus.Cancelled],
        [PaymentStatus.Paid] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
        [PaymentStatus.PartiallyRefunded] = [PaymentStatus.PartiallyRefunded, PaymentStatus.Refunded],
        [PaymentStatus.Failed] = [],
        [PaymentStatus.Cancelled] = [],
        [PaymentStatus.Refunded] = []
    };

    public static bool CanTransition(PaymentStatus from, PaymentStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyCollection<PaymentStatus> GetAllowedNext(PaymentStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];

    public static bool IsTerminal(PaymentStatus status) => TerminalStatuses.Contains(status);
}
