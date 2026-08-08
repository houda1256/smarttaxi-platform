using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record CashDeclarationResponse(
    Guid Id,
    Guid DriverId,
    Guid? AssignmentId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal ExpectedCash,
    decimal DeclaredCash,
    decimal Difference,
    string OperatingModel,
    string Status,
    DateTime SubmittedAt,
    Guid? ReviewedBy,
    DateTime? ReviewedAt)
{
    public static CashDeclarationResponse FromEntity(CashDeclaration declaration) => new(
        declaration.Id, declaration.DriverId, declaration.AssignmentId, declaration.PeriodStart, declaration.PeriodEnd,
        declaration.ExpectedCash, declaration.DeclaredCash, declaration.Difference, declaration.OperatingModel.ToString(),
        declaration.Status.ToString(), declaration.SubmittedAt, declaration.ReviewedBy, declaration.ReviewedAt);
}
