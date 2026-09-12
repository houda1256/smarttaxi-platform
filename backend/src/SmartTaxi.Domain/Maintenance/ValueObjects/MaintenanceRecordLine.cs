using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Maintenance.ValueObjects;

/// <summary>
/// A single service performed or part used during an intervention — a simple
/// line item, deliberately not a first-class SparePart/inventory entity
/// (explicitly out of scope for Module 9). Owned by MaintenanceRecord only;
/// immutable once the record is created.
/// </summary>
public sealed class MaintenanceRecordLine : ValueObject
{
    public string Description { get; }
    public bool IsPart { get; }
    public int Quantity { get; }
    public decimal UnitCost { get; }

    public MaintenanceRecordLine(string description, bool isPart, int quantity, decimal unitCost)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la ligne est requise.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("La quantité doit être strictement positive.");
        }

        if (unitCost < 0)
        {
            throw new ArgumentException("Le coût unitaire ne peut pas être négatif.");
        }

        Description = description.Trim();
        IsPart = isPart;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Description;
        yield return IsPart;
        yield return Quantity;
        yield return UnitCost;
    }
}
