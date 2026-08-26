using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Queries.GetRoadsideAssistanceRequestById;

/// <summary>Only the request's own requester or its currently selected partner may read it — never a plain permission-only gate.</summary>
public sealed class GetRoadsideAssistanceRequestByIdQueryHandler : IQueryHandler<GetRoadsideAssistanceRequestByIdQuery, Result<RoadsideAssistanceRequest>>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string ForbiddenError = "Vous n'avez pas accès à cette demande d'assistance routière.";

    private readonly IRoadsideAssistanceRequestRepository _repository;

    public GetRoadsideAssistanceRequestByIdQueryHandler(IRoadsideAssistanceRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RoadsideAssistanceRequest>> Handle(GetRoadsideAssistanceRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(query.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<RoadsideAssistanceRequest>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.RequesterUserId != query.RequestingUserId && request.SelectedPartnerUserId != query.RequestingUserId)
        {
            return Result<RoadsideAssistanceRequest>.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        return Result<RoadsideAssistanceRequest>.Success(request);
    }
}
