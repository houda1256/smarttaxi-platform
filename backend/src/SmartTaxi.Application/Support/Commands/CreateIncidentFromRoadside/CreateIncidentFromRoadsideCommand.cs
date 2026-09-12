using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromRoadside;

/// <summary>Manual, admin-triggered — no automatic hook from the RoadsideAssistance module itself. Idempotent via SourceType="RoadsideAssistanceRequest"/SourceId=RequestId.</summary>
public sealed record CreateIncidentFromRoadsideCommand(Guid RequestId, Guid AdminUserId, SupportIncidentSeverity Severity)
    : ICommand<Result<Guid>>;
