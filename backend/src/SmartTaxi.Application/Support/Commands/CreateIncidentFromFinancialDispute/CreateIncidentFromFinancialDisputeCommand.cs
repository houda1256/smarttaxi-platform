using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromFinancialDispute;

/// <summary>Manual, admin-triggered — no automatic hook from the Payments module itself. Idempotent via SourceType="FinancialDispute"/SourceId=DisputeId.</summary>
public sealed record CreateIncidentFromFinancialDisputeCommand(Guid DisputeId, Guid AdminUserId, SupportIncidentSeverity Severity)
    : ICommand<Result<Guid>>;
