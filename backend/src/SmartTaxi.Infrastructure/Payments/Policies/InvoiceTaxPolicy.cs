using Microsoft.Extensions.Options;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Infrastructure.Payments.Options;

namespace SmartTaxi.Infrastructure.Payments.Policies;

internal sealed class InvoiceTaxPolicy : IInvoiceTaxPolicy
{
    public decimal TaxPercentage { get; }

    public InvoiceTaxPolicy(IOptions<InvoiceTaxOptions> options)
    {
        TaxPercentage = options.Value.TaxPercentage;
    }
}
