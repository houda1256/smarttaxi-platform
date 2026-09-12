using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.CompleteMaintenance;

public sealed record MaintenanceRecordLineInput(string Description, bool IsPart, int Quantity, decimal UnitCost);

public sealed record CompleteMaintenanceCommand(
    Guid RequestId, Guid GarageUserId, decimal FinalCost, IReadOnlyCollection<MaintenanceRecordLineInput> Lines, string? Notes,
    DateOnly? NextRecommendedServiceDate, string? WarrantyInfo) : ICommand<Result>;
