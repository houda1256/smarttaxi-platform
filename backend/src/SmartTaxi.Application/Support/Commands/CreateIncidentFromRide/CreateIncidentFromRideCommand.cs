using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromRide;

/// <summary>Manual, admin-triggered — no automatic hook from the Ride module itself. Idempotent via SourceType="Ride"/SourceId=RideId: retrying returns the same incident.</summary>
public sealed record CreateIncidentFromRideCommand(
    Guid RideId, Guid AdminUserId, SupportIncidentType Type, SupportIncidentSeverity Severity, string? DescriptionOverride)
    : ICommand<Result<Guid>>;
