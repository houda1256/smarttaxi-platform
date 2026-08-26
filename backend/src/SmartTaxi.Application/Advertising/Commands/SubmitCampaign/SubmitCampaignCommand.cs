using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.SubmitCampaign;

public sealed record SubmitCampaignCommand(Guid CampaignId, Guid RequestingUserId) : ICommand<Result>;
