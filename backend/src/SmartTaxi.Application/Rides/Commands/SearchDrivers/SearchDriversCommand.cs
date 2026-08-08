using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.SearchDrivers;

public sealed record SearchDriversCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result<int>>;
