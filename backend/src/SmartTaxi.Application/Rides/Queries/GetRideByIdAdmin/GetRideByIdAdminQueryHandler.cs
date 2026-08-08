using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetRideByIdAdmin;

public sealed class GetRideByIdAdminQueryHandler : IQueryHandler<GetRideByIdAdminQuery, Result<RideSummary>>
{
    private const string NotFoundError = "Course introuvable.";

    private readonly IRideRepository _repository;

    public GetRideByIdAdminQueryHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<RideSummary>> Handle(GetRideByIdAdminQuery query, CancellationToken cancellationToken)
    {
        var ride = await _repository.GetByIdAsync(query.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<RideSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<RideSummary>.Success(RideSummary.FromEntity(ride));
    }
}
