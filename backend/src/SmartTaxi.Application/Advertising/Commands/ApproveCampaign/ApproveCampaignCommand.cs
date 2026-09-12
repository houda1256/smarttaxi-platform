using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ApproveCampaign;

public sealed record ApproveCampaignCommand(Guid CampaignId, Guid ReviewerUserId) : ICommand<Result>;
