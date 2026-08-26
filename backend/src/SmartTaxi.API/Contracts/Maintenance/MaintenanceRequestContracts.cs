using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.ValueObjects;

namespace SmartTaxi.API.Contracts.Maintenance;

public sealed record CreateMaintenanceRequestRequest(Guid VehicleId, Guid GarageUserId, string Description);

public sealed record RespondToMaintenanceRequestRequest(bool IsAccepted, string? RejectionReason);

public sealed record CancelMaintenanceRequestRequest(string? Reason);

public sealed record SubmitMaintenanceQuoteRequest(decimal EstimatedCost);

public sealed record RespondToMaintenanceQuoteRequest(bool IsAccepted);

public sealed record MaintenanceRecordLineRequest(string Description, bool IsPart, int Quantity, decimal UnitCost);

public sealed record CompleteMaintenanceRequest(
    decimal FinalCost, IReadOnlyCollection<MaintenanceRecordLineRequest> Lines, string? Notes, DateOnly? NextRecommendedServiceDate,
    string? WarrantyInfo);

public sealed record ForceCancelMaintenanceRequestRequest(string Reason);

public sealed record MaintenanceRequestResponse(
    Guid Id, Guid VehicleId, Guid OwnerUserId, Guid GarageUserId, string Description, string Status, DateTime RequestedAtUtc,
    DateTime? ConfirmedAtUtc, string? GarageRejectionReason, decimal? EstimatedCost, DateTime? QuoteSubmittedAtUtc,
    DateTime? QuoteAcceptedAtUtc, DateTime? VehicleReceivedAtUtc, DateTime? WorkStartedAtUtc, decimal? FinalCost,
    DateTime? CompletedAtUtc, DateTime? SettledAtUtc, DateTime? CancelledAtUtc, string? CancellationReason, Guid? CancelledByUserId,
    DateTime UpdatedAtUtc)
{
    public static MaintenanceRequestResponse FromEntity(MaintenanceRequest request) => new(
        request.Id, request.VehicleId, request.OwnerUserId, request.GarageUserId, request.Description, request.Status.ToString(),
        request.RequestedAtUtc, request.ConfirmedAtUtc, request.GarageRejectionReason, request.EstimatedCost,
        request.QuoteSubmittedAtUtc, request.QuoteAcceptedAtUtc, request.VehicleReceivedAtUtc, request.WorkStartedAtUtc,
        request.FinalCost, request.CompletedAtUtc, request.SettledAtUtc, request.CancelledAtUtc, request.CancellationReason,
        request.CancelledByUserId, request.UpdatedAtUtc);
}

public sealed record MaintenanceRecordLineResponse(string Description, bool IsPart, int Quantity, decimal UnitCost)
{
    public static MaintenanceRecordLineResponse FromValueObject(MaintenanceRecordLine line) =>
        new(line.Description, line.IsPart, line.Quantity, line.UnitCost);
}

public sealed record MaintenanceRecordResponse(
    Guid Id, Guid MaintenanceRequestId, Guid VehicleId, Guid OwnerUserId, Guid GarageUserId, DateOnly InterventionDate,
    int? MileageAtCompletion, IReadOnlyCollection<MaintenanceRecordLineResponse> Lines, decimal FinalCost, string? Notes,
    DateOnly? NextRecommendedServiceDate, string? WarrantyInfo, DateTime CreatedAtUtc)
{
    public static MaintenanceRecordResponse FromEntity(MaintenanceRecord record) => new(
        record.Id, record.MaintenanceRequestId, record.VehicleId, record.OwnerUserId, record.GarageUserId, record.InterventionDate,
        record.MileageAtCompletion, record.Lines.Select(MaintenanceRecordLineResponse.FromValueObject).ToList(), record.FinalCost,
        record.Notes, record.NextRecommendedServiceDate, record.WarrantyInfo, record.CreatedAtUtc);
}
