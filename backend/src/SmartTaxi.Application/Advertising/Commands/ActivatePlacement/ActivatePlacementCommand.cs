using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ActivatePlacement;

public sealed record ActivatePlacementCommand(Guid PlacementId) : ICommand<Result>;
