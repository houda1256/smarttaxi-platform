using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetMyDriverProfile;

public sealed class GetMyDriverProfileQueryHandler : IQueryHandler<GetMyDriverProfileQuery, Result<DriverProfileSummary>>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";

    private readonly IDriverProfileRepository _repository;

    public GetMyDriverProfileQueryHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DriverProfileSummary>> Handle(GetMyDriverProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<DriverProfileSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<DriverProfileSummary>.Success(DriverProfileSummary.FromEntity(profile));
    }
}
