using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record OpenFinancialDisputeRequest(
    FinancialDisputeCategory Category, Guid? RelatedPaymentId, Guid? RelatedInvoiceId, Guid? RelatedPayoutId,
    decimal DisputedAmount, string Currency, string Description, string? EvidenceReference);

public sealed record ResolveFinancialDisputeRequest(string Resolution, DisputeResolutionOutcome Outcome);

public sealed record RejectFinancialDisputeRequest(string Resolution);
