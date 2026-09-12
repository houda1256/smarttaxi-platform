using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

/// <summary>The lines themselves live in their own table (GroupedInvoiceLine) rather than an owned collection, so the "Ride already invoiced" uniqueness check can be enforced by a simple DB index independent of which GroupedInvoice a line belongs to.</summary>
public sealed class GroupedInvoice : AggregateRoot
{
    public Guid BusinessCustomerId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public GroupedInvoicePeriodType PeriodType { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public GroupedInvoiceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private GroupedInvoice()
    {
    }

    private GroupedInvoice(
        Guid businessCustomerId, GroupedInvoicePeriodType periodType, DateOnly periodStart, DateOnly periodEnd,
        decimal subtotal, decimal taxAmount, string currency, DateTime issueDate, DateTime dueDate)
        : base(Guid.NewGuid())
    {
        BusinessCustomerId = businessCustomerId;
        InvoiceNumber = GenerateInvoiceNumber(issueDate);
        PeriodType = periodType;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = subtotal + taxAmount;
        Currency = currency;
        IssueDate = issueDate;
        DueDate = dueDate;
        Status = GroupedInvoiceStatus.Issued;
        CreatedAt = issueDate;
    }

    public static GroupedInvoice Generate(
        Guid businessCustomerId, GroupedInvoicePeriodType periodType, DateOnly periodStart, DateOnly periodEnd,
        decimal subtotal, decimal taxAmount, string currency, DateTime issueDate, DateTime dueDate)
    {
        if (periodEnd < periodStart)
        {
            throw new ArgumentException("La fin de période ne peut pas précéder le début.");
        }

        if (subtotal < 0 || taxAmount < 0)
        {
            throw new ArgumentException("Les montants ne peuvent pas être négatifs.");
        }

        if (dueDate < issueDate)
        {
            throw new ArgumentException("La date d'échéance ne peut pas précéder la date d'émission.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        return new GroupedInvoice(businessCustomerId, periodType, periodStart, periodEnd, subtotal, taxAmount, currency.Trim().ToUpperInvariant(), issueDate, dueDate);
    }

    private static string GenerateInvoiceNumber(DateTime utcNow) =>
        $"GINV-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
