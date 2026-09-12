using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetDriverProfileById;

public sealed class GetDriverProfileByIdQueryHandler : IQueryHandler<GetDriverProfileByIdQuery, Result<DriverProfileSummary>>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";

    private readonly IDriverProfileRepository _repository;

    public GetDriverProfileByIdQueryHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DriverProfileSummary>> Handle(GetDriverProfileByIdQuery query, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(query.DriverProfileId, cancellationToken);

        if (profile is null)
        {
            return Result<DriverProfileSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<DriverProfileSummary>.Success(DriverProfileSummary.FromEntity(profile));
    }
}
