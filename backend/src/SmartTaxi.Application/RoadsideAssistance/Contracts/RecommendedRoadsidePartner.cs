namespace SmartTaxi.Application.RoadsideAssistance.Contracts;

/// <summary>
/// Requester-facing recommendation DTO — deliberately narrow (approved plan):
/// no rating (no trustworthy field exists), no ETA (no route/speed model
/// exists for Roadside), no availability/busy state (not modeled). DistanceKm
/// is null whenever the partner has no recorded base coordinates — never
/// defaulted or estimated.
/// </summary>
public sealed record RecommendedRoadsidePartner(Guid PartnerUserId, string BusinessName, string? City, double? DistanceKm);
