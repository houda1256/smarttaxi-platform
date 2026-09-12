using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Application.Payments.BusinessCustomers.Commands.RegisterBusinessCustomer;

public sealed record RegisterBusinessCustomerCommand(
    string LegalName, string TaxIdentifier, string BillingAddress, string ContactPersonName, string ContactPersonEmail,
    string? ContactPersonPhone, DeferredPaymentTerm PaymentTerms, decimal CreditLimit) : ICommand<Result<Guid>>;
