using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeInvoiceTaxPolicy : IInvoiceTaxPolicy
{
    public decimal TaxPercentage { get; init; } = 0m;
}
