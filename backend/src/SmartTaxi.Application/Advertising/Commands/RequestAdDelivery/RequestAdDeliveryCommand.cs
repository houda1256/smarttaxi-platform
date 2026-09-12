using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RequestAdDelivery;

public sealed record RequestAdDeliveryCommand(Guid CampaignId, Guid PlacementId, Guid RequestingUserId) : ICommand<Result<string>>;
