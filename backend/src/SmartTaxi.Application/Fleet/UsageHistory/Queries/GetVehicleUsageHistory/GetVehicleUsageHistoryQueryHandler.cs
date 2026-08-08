using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;

namespace SmartTaxi.Application.Fleet.UsageHistory.Queries.GetVehicleUsageHistory;

public sealed class GetVehicleUsageHistoryQueryHandler
    : IQueryHandler<GetVehicleUsageHistoryQuery, IReadOnlyCollection<VehicleUsageRecordSummary>>
{
    private readonly IVehicleUsageRecordRepository _repository;

    public GetVehicleUsageHistoryQueryHandler(IVehicleUsageRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<VehicleUsageRecordSummary>> Handle(
        GetVehicleUsageHistoryQuery query, CancellationToken cancellationToken)
    {
        var records = await _repository.GetForVehicleAsync(query.VehicleId, cancellationToken);

        return records.Select(VehicleUsageRecordSummary.FromEntity).ToList();
    }
}
