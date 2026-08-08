namespace SmartTaxi.Infrastructure.Payments.Options;

public sealed class InvoiceTaxOptions
{
    public const string SectionName = "InvoiceTax";

    public decimal TaxPercentage { get; init; } = 0m;
}
