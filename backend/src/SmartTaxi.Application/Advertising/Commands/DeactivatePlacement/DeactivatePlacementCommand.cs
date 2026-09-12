using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.DeactivatePlacement;

public sealed record DeactivatePlacementCommand(Guid PlacementId) : ICommand<Result>;
