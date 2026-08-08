using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record FinancialDisputeResponse(
    Guid Id,
    string Category,
    Guid? RelatedPaymentId,
    Guid? RelatedInvoiceId,
    Guid? RelatedPayoutId,
    decimal DisputedAmount,
    string Currency,
    string Description,
    string? EvidenceReference,
    string Status,
    Guid RaisedBy,
    Guid? AssignedFinanceManagerId,
    string? Resolution,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static FinancialDisputeResponse FromEntity(FinancialDispute dispute) => new(
        dispute.Id, dispute.Category.ToString(), dispute.RelatedPaymentId, dispute.RelatedInvoiceId, dispute.RelatedPayoutId,
        dispute.DisputedAmount, dispute.Currency, dispute.Description, dispute.EvidenceReference, dispute.Status.ToString(),
        dispute.RaisedBy, dispute.AssignedFinanceManagerId, dispute.Resolution, dispute.ResolvedAt, dispute.CreatedAt, dispute.UpdatedAt);
}
