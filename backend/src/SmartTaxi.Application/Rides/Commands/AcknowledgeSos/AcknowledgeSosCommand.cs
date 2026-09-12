using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.AcknowledgeSos;

/// <summary>No ownership check — gated purely by a SecurityOfficer/admin permission at the API layer.</summary>
public sealed record AcknowledgeSosCommand(Guid SafetyEventId) : ICommand<Result>;
