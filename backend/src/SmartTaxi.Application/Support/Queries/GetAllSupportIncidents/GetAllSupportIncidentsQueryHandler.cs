using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetAllSupportIncidents;

public sealed class GetAllSupportIncidentsQueryHandler : IQueryHandler<GetAllSupportIncidentsQuery, PagedResult<SupportIncident>>
{
    private readonly ISupportIncidentRepository _incidentRepository;

    public GetAllSupportIncidentsQueryHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public Task<PagedResult<SupportIncident>> Handle(GetAllSupportIncidentsQuery query, CancellationToken cancellationToken) =>
        _incidentRepository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
