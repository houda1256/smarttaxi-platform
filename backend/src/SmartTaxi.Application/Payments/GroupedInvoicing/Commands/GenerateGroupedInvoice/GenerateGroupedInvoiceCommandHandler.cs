using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.BusinessCustomers.Abstractions;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Application.Payments.Taxes.Abstractions;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Commands.GenerateGroupedInvoice;

/// <summary>
/// The Ride-duplication guard (AnyAlreadyInvoicedAsync) and the credit-limit
/// guard (TryIncreaseCreditUsageAsync) are both re-verified atomically at the
/// point of writing in the real Infrastructure implementation — this handler
/// additionally compensates (cancels the invoice, releases the credit
/// reservation) if the line-insert loses a race after the invoice row and
/// credit reservation were already committed, since those three writes are
/// not one single database statement.
/// </summary>
public sealed class GenerateGroupedInvoiceCommandHandler : ICommandHandler<GenerateGroupedInvoiceCommand, Result<Guid>>
{
    private const string CustomerNotFoundError = "Client entreprise introuvable.";
    private const string CustomerNotActiveError = "Ce client entreprise n'est pas actif.";
    private const string NoRidesError = "Au moins une course est requise pour générer une facture groupée.";
    private const string AlreadyInvoicedError = "Une ou plusieurs courses ont déjà été facturées.";
    private const string CreditLimitExceededError = "Cette facture dépasserait la limite de crédit du client entreprise.";

    private readonly IBusinessCustomerRepository _businessCustomerRepository;
    private readonly IGroupedInvoiceRepository _invoiceRepository;
    private readonly IGroupedInvoiceLineRepository _lineRepository;
    private readonly ITaxRuleRepository _taxRuleRepository;

    public GenerateGroupedInvoiceCommandHandler(
        IBusinessCustomerRepository businessCustomerRepository, IGroupedInvoiceRepository invoiceRepository,
        IGroupedInvoiceLineRepository lineRepository, ITaxRuleRepository taxRuleRepository)
    {
        _businessCustomerRepository = businessCustomerRepository;
        _invoiceRepository = invoiceRepository;
        _lineRepository = lineRepository;
        _taxRuleRepository = taxRuleRepository;
    }

    public async Task<Result<Guid>> Handle(GenerateGroupedInvoiceCommand command, CancellationToken cancellationToken)
    {
        if (command.Rides.Count == 0)
        {
            return Result<Guid>.Failure(NoRidesError, ErrorType.Validation);
        }

        var customer = await _businessCustomerRepository.GetByIdAsync(command.BusinessCustomerId, cancellationToken);

        if (customer is null)
        {
            return Result<Guid>.Failure(CustomerNotFoundError, ErrorType.NotFound);
        }

        if (customer.Status != BusinessCustomerStatus.Active)
        {
            return Result<Guid>.Failure(CustomerNotActiveError, ErrorType.Conflict);
        }

        var rideIds = command.Rides.Select(r => r.RideId).ToList();

        if (await _lineRepository.AnyAlreadyInvoicedAsync(rideIds, cancellationToken))
        {
            return Result<Guid>.Failure(AlreadyInvoicedError, ErrorType.Conflict);
        }

        var subtotal = command.Rides.Sum(r => r.Amount);
        var issueDate = DateTime.UtcNow;
        var taxRule = await _taxRuleRepository.GetApplicableRuleAsync("Rides", DateOnly.FromDateTime(issueDate), cancellationToken);
        var taxAmount = taxRule is null ? 0m : Math.Round(subtotal * taxRule.TaxRate / 100m, 2);
        var totalAmount = subtotal + taxAmount;

        var isDeferred = customer.PaymentTerms != DeferredPaymentTerm.Immediate;

        if (isDeferred && !await _businessCustomerRepository.TryIncreaseCreditUsageAsync(customer.Id, totalAmount, issueDate, cancellationToken))
        {
            return Result<Guid>.Failure(CreditLimitExceededError, ErrorType.Conflict);
        }

        GroupedInvoice invoice;

        try
        {
            invoice = GroupedInvoice.Generate(
                customer.Id, command.PeriodType, command.PeriodStart, command.PeriodEnd, subtotal, taxAmount,
                command.Currency, issueDate, command.DueDate);
        }
        catch (ArgumentException ex)
        {
            if (isDeferred)
            {
                await _businessCustomerRepository.TryDecreaseCreditUsageAsync(customer.Id, totalAmount, issueDate, cancellationToken);
            }

            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);

        var lines = command.Rides.Select(r => new GroupedInvoiceLine(invoice.Id, r.RideId, r.RideNumber, r.Amount, issueDate)).ToList();
        var linesAdded = await _lineRepository.TryAddRangeAsync(lines, cancellationToken);

        if (!linesAdded)
        {
            await _invoiceRepository.TryCancelAsync(invoice.Id, issueDate, cancellationToken);

            if (isDeferred)
            {
                await _businessCustomerRepository.TryDecreaseCreditUsageAsync(customer.Id, totalAmount, issueDate, cancellationToken);
            }

            return Result<Guid>.Failure(AlreadyInvoicedError, ErrorType.Conflict);
        }

        return Result<Guid>.Success(invoice.Id);
    }
}
