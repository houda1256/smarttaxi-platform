using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ResolveSos;

public sealed record ResolveSosCommand(Guid SafetyEventId) : ICommand<Result>;
