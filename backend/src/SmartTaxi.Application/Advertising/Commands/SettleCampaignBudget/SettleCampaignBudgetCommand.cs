using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;

public sealed record SettleCampaignBudgetCommand(Guid CampaignId, Guid AdminUserId) : ICommand<Result>;
