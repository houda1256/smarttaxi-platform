using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.UsageHistory.Queries.GetDriverUsageHistory;

public sealed record GetDriverUsageHistoryQuery(Guid DriverId) : IQuery<IReadOnlyCollection<VehicleUsageRecordSummary>>;
