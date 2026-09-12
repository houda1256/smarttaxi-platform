using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.MarkGroupedInvoicePaid;

/// <summary>Releasing the credit reservation only when PaymentTerms was ever deferred mirrors exactly how GenerateGroupedInvoiceCommandHandler only reserved it in that same case.</summary>
public sealed class MarkGroupedInvoicePaidCommandHandler : ICommandHandler<MarkGroupedInvoicePaidCommand, Result>
{
    private const string NotFoundError = "Facture groupée introuvable.";
    private const string NotPayableError = "Cette facture ne peut pas être marquée comme payée.";

    private readonly IGroupedInvoiceRepository _invoiceRepository;
    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public MarkGroupedInvoicePaidCommandHandler(IGroupedInvoiceRepository invoiceRepository, IBusinessCustomerRepository businessCustomerRepository)
    {
        _invoiceRepository = invoiceRepository;
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result> Handle(MarkGroupedInvoicePaidCommand command, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(command.GroupedInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var marked = await _invoiceRepository.TryMarkPaidAsync(command.GroupedInvoiceId, utcNow, cancellationToken);

        if (!marked)
        {
            return Result.Failure(NotPayableError, ErrorType.Conflict);
        }

        var customer = await _businessCustomerRepository.GetByIdAsync(invoice.BusinessCustomerId, cancellationToken);

        if (customer is not null && customer.PaymentTerms != DeferredPaymentTerm.Immediate)
        {
            await _businessCustomerRepository.TryDecreaseCreditUsageAsync(customer.Id, invoice.TotalAmount, utcNow, cancellationToken);
        }

        return Result.Success();
    }
}
