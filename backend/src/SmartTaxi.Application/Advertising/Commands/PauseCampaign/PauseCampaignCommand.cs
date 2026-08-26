using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.PauseCampaign;

public sealed record PauseCampaignCommand(Guid CampaignId, Guid RequestingUserId) : ICommand<Result>;
