using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Identity.DataRequests;

/// <summary>Deliberately excludes ResultReference from admin-queue listings — see GetMyPersonalDataRequestsQuery for the owner-only exception.</summary>
public sealed record PersonalDataRequestSummary(
    Guid Id,
    Guid UserId,
    PersonalDataRequestType RequestType,
    PersonalDataRequestStatus Status,
    DateTime RequestedAt,
    Guid? ProcessedBy,
    DateTime? ProcessedAt,
    string? ProcessingNotes,
    bool HasExportResult)
{
    public static PersonalDataRequestSummary FromEntity(PersonalDataRequest request) => new(
        request.Id,
        request.UserId,
        request.RequestType,
        request.Status,
        request.RequestedAt,
        request.ProcessedBy,
        request.ProcessedAt,
        request.ProcessingNotes,
        request.ResultReference is not null);
}
