using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ResumeCampaign;

public sealed record ResumeCampaignCommand(Guid CampaignId, Guid RequestingUserId) : ICommand<Result>;
