using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetRideById;

/// <summary>Visible to the Ride's Customer or its selected Driver — never to an unrelated user.</summary>
public sealed class GetRideByIdQueryHandler : IQueryHandler<GetRideByIdQuery, Result<RideSummary>>
{
    private const string NotFoundError = "Course introuvable.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public GetRideByIdQueryHandler(IRideRepository rideRepository, IDriverProfileRepository driverRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result<RideSummary>> Handle(GetRideByIdQuery query, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(query.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<RideSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.CustomerId != query.RequestingUserId)
        {
            var isRequestingDriver = false;

            if (ride.SelectedDriverId is not null)
            {
                var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
                isRequestingDriver = driver is not null && driver.UserId == query.RequestingUserId;
            }

            if (!isRequestingDriver)
            {
                return Result<RideSummary>.Failure(NotFoundError, ErrorType.NotFound);
            }
        }

        return Result<RideSummary>.Success(RideSummary.FromEntity(ride));
    }
}
