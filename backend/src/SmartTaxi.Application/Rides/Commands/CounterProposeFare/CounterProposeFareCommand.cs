using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CounterProposeFare;

public sealed record CounterProposeFareCommand(Guid RequestingUserId, Guid RideId, decimal Amount) : ICommand<Result<Guid>>;
