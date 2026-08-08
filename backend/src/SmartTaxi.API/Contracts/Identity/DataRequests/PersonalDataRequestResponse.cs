using SmartTaxi.Application.Identity.DataRequests;

namespace SmartTaxi.API.Contracts.Identity.DataRequests;

public sealed record PersonalDataRequestResponse(
    Guid Id,
    Guid UserId,
    string RequestType,
    string Status,
    DateTime RequestedAt,
    Guid? ProcessedBy,
    DateTime? ProcessedAt,
    string? ProcessingNotes,
    bool HasExportResult)
{
    public static PersonalDataRequestResponse FromSummary(PersonalDataRequestSummary summary) => new(
        summary.Id,
        summary.UserId,
        summary.RequestType.ToString(),
        summary.Status.ToString(),
        summary.RequestedAt,
        summary.ProcessedBy,
        summary.ProcessedAt,
        summary.ProcessingNotes,
        summary.HasExportResult);
}
