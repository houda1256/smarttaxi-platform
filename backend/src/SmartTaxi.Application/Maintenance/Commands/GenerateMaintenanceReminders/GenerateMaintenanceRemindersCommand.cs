using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.GenerateMaintenanceReminders;

/// <summary>Manual/admin-triggered sweep — no background scheduler, same convention as Fleet's ExpireVehicleDocumentsCommand and Advertising's ProcessScheduledCampaigns.</summary>
public sealed record GenerateMaintenanceRemindersCommand : ICommand<Result<int>>;
