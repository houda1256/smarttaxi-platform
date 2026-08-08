using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;

namespace SmartTaxi.Application.Fleet.UsageHistory.Queries.GetDriverUsageHistory;

public sealed class GetDriverUsageHistoryQueryHandler
    : IQueryHandler<GetDriverUsageHistoryQuery, IReadOnlyCollection<VehicleUsageRecordSummary>>
{
    private readonly IVehicleUsageRecordRepository _repository;

    public GetDriverUsageHistoryQueryHandler(IVehicleUsageRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<VehicleUsageRecordSummary>> Handle(
        GetDriverUsageHistoryQuery query, CancellationToken cancellationToken)
    {
        var records = await _repository.GetForDriverAsync(query.DriverId, cancellationToken);

        return records.Select(VehicleUsageRecordSummary.FromEntity).ToList();
    }
}
