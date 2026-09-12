using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RejectCampaign;

public sealed record RejectCampaignCommand(Guid CampaignId, Guid ReviewerUserId, string Reason) : ICommand<Result>;
