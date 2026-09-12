using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Payments.Disputes.Commands.OpenFinancialDispute;

public sealed record OpenFinancialDisputeCommand(
    Guid RaisedBy, FinancialDisputeCategory Category, Guid? RelatedPaymentId, Guid? RelatedInvoiceId, Guid? RelatedPayoutId,
    decimal DisputedAmount, string Currency, string Description, string? EvidenceReference) : ICommand<Result<Guid>>;
