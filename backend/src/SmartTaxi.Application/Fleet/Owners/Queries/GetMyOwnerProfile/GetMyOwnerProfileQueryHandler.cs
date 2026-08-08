using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Owners.Abstractions;

namespace SmartTaxi.Application.Fleet.Owners.Queries.GetMyOwnerProfile;

public sealed class GetMyOwnerProfileQueryHandler : IQueryHandler<GetMyOwnerProfileQuery, Result<OwnerProfileSummary>>
{
    private const string NotFoundError = "Profil propriétaire introuvable.";

    private readonly ITaxiOwnerProfileRepository _repository;

    public GetMyOwnerProfileQueryHandler(ITaxiOwnerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<OwnerProfileSummary>> Handle(GetMyOwnerProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<OwnerProfileSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<OwnerProfileSummary>.Success(OwnerProfileSummary.FromEntity(profile));
    }
}
