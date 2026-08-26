using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.Application.Advertising.Commands.CreatePlacement;

public sealed record CreatePlacementCommand(string Code, string Name, string Description, IReadOnlyCollection<AdMediaType> SupportedMediaTypes)
    : ICommand<Result<Guid>>;
