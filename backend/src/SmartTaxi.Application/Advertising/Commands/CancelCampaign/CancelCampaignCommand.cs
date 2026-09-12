using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.CancelCampaign;

public sealed record CancelCampaignCommand(Guid CampaignId, Guid RequestingUserId) : ICommand<Result>;
