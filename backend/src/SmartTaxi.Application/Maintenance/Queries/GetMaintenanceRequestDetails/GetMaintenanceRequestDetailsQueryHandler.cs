using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMaintenanceRequestDetails;

/// <summary>Only the request's own owner or its assigned garage may read it — never a plain permission-only gate.</summary>
public sealed class GetMaintenanceRequestDetailsQueryHandler : IQueryHandler<GetMaintenanceRequestDetailsQuery, Result<MaintenanceRequest>>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string ForbiddenError = "Vous n'avez pas accès à cette demande de maintenance.";

    private readonly IMaintenanceRequestRepository _repository;

    public GetMaintenanceRequestDetailsQueryHandler(IMaintenanceRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<MaintenanceRequest>> Handle(GetMaintenanceRequestDetailsQuery query, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(query.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<MaintenanceRequest>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.OwnerUserId != query.RequestingUserId && request.GarageUserId != query.RequestingUserId)
        {
            return Result<MaintenanceRequest>.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        return Result<MaintenanceRequest>.Success(request);
    }
}
