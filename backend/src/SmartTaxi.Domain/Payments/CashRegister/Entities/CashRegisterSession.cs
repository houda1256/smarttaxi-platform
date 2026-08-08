using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Domain.Payments.CashRegister.Entities;

/// <summary>
/// Closing (Open→Closing→Closed) is an atomic repository-level guard, not a
/// domain method — the Application layer computes ClosingExpectedBalance
/// from the session's own CashMovement rows and requires a
/// DifferenceReason whenever ClosingActualBalance departs from it (the
/// master prompt's "cash differences require justification" rule).
/// </summary>
public sealed class CashRegisterSession : AggregateRoot
{
    public Guid CashRegisterId { get; private set; }
    public Guid OpenedBy { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal? ClosingExpectedBalance { get; private set; }
    public decimal? ClosingActualBalance { get; private set; }
    public decimal? Difference { get; private set; }
    public string? DifferenceReason { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public CashRegisterSessionStatus Status { get; private set; }

    private CashRegisterSession()
    {
    }

    private CashRegisterSession(Guid cashRegisterId, Guid openedBy, decimal openingBalance, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CashRegisterId = cashRegisterId;
        OpenedBy = openedBy;
        OpeningBalance = openingBalance;
        Status = CashRegisterSessionStatus.Open;
        OpenedAt = utcNow;
    }

    public static CashRegisterSession Open(Guid cashRegisterId, Guid openedBy, decimal openingBalance, DateTime utcNow)
    {
        if (openingBalance < 0)
        {
            throw new ArgumentException("Le solde d'ouverture ne peut pas être négatif.");
        }

        return new CashRegisterSession(cashRegisterId, openedBy, openingBalance, utcNow);
    }
}
