using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Queries.GetSupportIncidentById;

public sealed class GetSupportIncidentByIdQueryHandler : IQueryHandler<GetSupportIncidentByIdQuery, Result<SupportIncident>>
{
    private const string NotFoundError = "Incident introuvable.";

    private readonly ISupportIncidentRepository _incidentRepository;

    public GetSupportIncidentByIdQueryHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result<SupportIncident>> Handle(GetSupportIncidentByIdQuery query, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(query.IncidentId, cancellationToken);

        return incident is null
            ? Result<SupportIncident>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<SupportIncident>.Success(incident);
    }
}
