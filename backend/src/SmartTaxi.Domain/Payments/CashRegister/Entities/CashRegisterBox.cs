using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Payments.CashRegister.Entities;

/// <summary>
/// Named CashRegisterBox (not CashRegister) purely to avoid a class/namespace
/// name collision with the enclosing SmartTaxi.Domain.Payments.CashRegister
/// namespace — the master prompt's own entity name. One box per Owner
/// (TaxiOwner or Platform-run agency); its FinancialAccount (AccountType =
/// CashRegister) is what actually accrues balance, tracked separately.
/// </summary>
public sealed class CashRegisterBox : AggregateRoot
{
    public Guid OwnerId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private CashRegisterBox()
    {
    }

    private CashRegisterBox(Guid ownerId, string label, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        OwnerId = ownerId;
        Label = label;
        CreatedAt = utcNow;
    }

    public static CashRegisterBox Register(Guid ownerId, string label, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Un libellé est requis pour la caisse.");
        }

        return new CashRegisterBox(ownerId, label, utcNow);
    }
}
