using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ReactivateCampaign;

public sealed record ReactivateCampaignCommand(Guid CampaignId, Guid AdminUserId) : ICommand<Result>;
