using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RequestCampaignChanges;

public sealed record RequestCampaignChangesCommand(Guid CampaignId, Guid ReviewerUserId, string Reason) : ICommand<Result>;
