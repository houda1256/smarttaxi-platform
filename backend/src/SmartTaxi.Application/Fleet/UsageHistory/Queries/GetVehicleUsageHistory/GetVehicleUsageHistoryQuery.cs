using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.UsageHistory.Queries.GetVehicleUsageHistory;

public sealed record GetVehicleUsageHistoryQuery(Guid VehicleId) : IQuery<IReadOnlyCollection<VehicleUsageRecordSummary>>;
