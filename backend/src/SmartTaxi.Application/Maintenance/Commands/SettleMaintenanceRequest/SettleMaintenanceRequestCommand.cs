using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;

/// <summary>Admin-only (see the settlement-authorization decision: mirrors Advertising's SettleCampaignBudget, which is Admin-gated, not self-service by the payer or payee).</summary>
public sealed record SettleMaintenanceRequestCommand(Guid RequestId, Guid AdminUserId) : ICommand<Result>;
