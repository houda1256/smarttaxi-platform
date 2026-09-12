namespace SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

/// <summary>One row per Ride included in a GroupedInvoice — a DB-level unique index on RideId (Infrastructure) is what actually guarantees a Ride can never be invoiced twice, across any GroupedInvoice, ever.</summary>
public sealed class GroupedInvoiceLine
{
    public Guid Id { get; private set; }
    public Guid GroupedInvoiceId { get; private set; }
    public Guid RideId { get; private set; }
    public string RideNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private GroupedInvoiceLine()
    {
    }

    public GroupedInvoiceLine(Guid groupedInvoiceId, Guid rideId, string rideNumber, decimal amount, DateTime utcNow)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Le montant d'une ligne de facture groupée ne peut pas être négatif.");
        }

        Id = Guid.NewGuid();
        GroupedInvoiceId = groupedInvoiceId;
        RideId = rideId;
        RideNumber = rideNumber;
        Amount = amount;
        CreatedAt = utcNow;
    }
}
