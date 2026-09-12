using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.CancelGroupedInvoice;

public sealed class CancelGroupedInvoiceCommandHandler : ICommandHandler<CancelGroupedInvoiceCommand, Result>
{
    private const string NotFoundError = "Facture groupée introuvable.";
    private const string NotCancellableError = "Cette facture ne peut pas être annulée.";

    private readonly IGroupedInvoiceRepository _invoiceRepository;
    private readonly IBusinessCustomerRepository _businessCustomerRepository;

    public CancelGroupedInvoiceCommandHandler(IGroupedInvoiceRepository invoiceRepository, IBusinessCustomerRepository businessCustomerRepository)
    {
        _invoiceRepository = invoiceRepository;
        _businessCustomerRepository = businessCustomerRepository;
    }

    public async Task<Result> Handle(CancelGroupedInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(command.GroupedInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var cancelled = await _invoiceRepository.TryCancelAsync(command.GroupedInvoiceId, utcNow, cancellationToken);

        if (!cancelled)
        {
            return Result.Failure(NotCancellableError, ErrorType.Conflict);
        }

        var customer = await _businessCustomerRepository.GetByIdAsync(invoice.BusinessCustomerId, cancellationToken);

        if (customer is not null && customer.PaymentTerms != DeferredPaymentTerm.Immediate)
        {
            await _businessCustomerRepository.TryDecreaseCreditUsageAsync(customer.Id, invoice.TotalAmount, utcNow, cancellationToken);
        }

        return Result.Success();
    }
}
