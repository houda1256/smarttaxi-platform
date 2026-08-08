namespace SmartTaxi.API.Contracts.Payments;

public sealed record SubmitCashDeclarationRequest(
    Guid OwnerId, Guid? AssignmentId, DateOnly PeriodStart, DateOnly PeriodEnd, decimal ExpectedCash, decimal DeclaredCash);
