using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromMaintenance;

/// <summary>Manual, admin-triggered — no automatic hook from the Maintenance module itself. Idempotent via SourceType="MaintenanceRequest"/SourceId=RequestId.</summary>
public sealed record CreateIncidentFromMaintenanceCommand(Guid RequestId, Guid AdminUserId, SupportIncidentSeverity Severity)
    : ICommand<Result<Guid>>;
