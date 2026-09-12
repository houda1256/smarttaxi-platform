namespace SmartTaxi.Application.Payments.Abstractions;

/// <summary>Configurable, never hardcoded. Payment.FinalFareAmount is treated as tax-inclusive; Subtotal/TaxAmount on the Invoice are a breakdown of that same total, never an addition on top of what the Customer actually paid.</summary>
public interface IInvoiceTaxPolicy
{
    decimal TaxPercentage { get; }
}
