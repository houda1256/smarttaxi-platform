using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Alerts.Abstractions;

namespace SmartTaxi.Application.Fleet.Alerts.Queries.GetFleetAlerts;

public sealed class GetFleetAlertsQueryHandler : IQueryHandler<GetFleetAlertsQuery, IReadOnlyCollection<FleetAlertSummary>>
{
    private readonly IFleetAlertRepository _repository;

    public GetFleetAlertsQueryHandler(IFleetAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<FleetAlertSummary>> Handle(GetFleetAlertsQuery query, CancellationToken cancellationToken)
    {
        var alerts = await _repository.GetForOwnerAsync(query.OwnerId, query.Status, cancellationToken);

        return alerts.Select(FleetAlertSummary.FromEntity).ToList();
    }
}
