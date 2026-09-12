using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.UpdatePlacement;

public sealed record UpdatePlacementCommand(Guid PlacementId, string Name, string Description, IReadOnlyCollection<AdMediaType> SupportedMediaTypes)
    : ICommand<Result>;
