using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Domain.Payments.CashRegister.Entities;

/// <summary>Immutable, append-only — the CashRegisterSession's ClosingExpectedBalance is always computed by summing these, never stored redundantly as a running total.</summary>
public sealed class CashMovement
{
    public Guid Id { get; private set; }
    public Guid CashRegisterSessionId { get; private set; }
    public CashMovementType MovementType { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid RecordedBy { get; private set; }

    private CashMovement()
    {
    }

    public CashMovement(Guid cashRegisterSessionId, CashMovementType movementType, decimal amount, string? description, Guid recordedBy, DateTime utcNow)
    {
        if (amount == 0)
        {
            throw new ArgumentException("Le montant d'un mouvement de caisse ne peut pas être nul.");
        }

        Id = Guid.NewGuid();
        CashRegisterSessionId = cashRegisterSessionId;
        MovementType = movementType;
        Amount = amount;
        Description = description;
        RecordedAt = utcNow;
        RecordedBy = recordedBy;
    }
}
