using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ProposeFare;

public sealed record ProposeFareCommand(Guid RequestingUserId, Guid RideId, decimal Amount) : ICommand<Result<Guid>>;
