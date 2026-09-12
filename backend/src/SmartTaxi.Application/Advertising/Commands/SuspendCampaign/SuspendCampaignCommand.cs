using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.SuspendCampaign;

public sealed record SuspendCampaignCommand(Guid CampaignId, Guid AdminUserId, string Reason) : ICommand<Result>;
