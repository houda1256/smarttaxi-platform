using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideRatings;

public sealed class GetRideRatingsQueryHandler : IQueryHandler<GetRideRatingsQuery, Result<IReadOnlyCollection<RideRating>>>
{
    private readonly IRideRatingRepository _ratingRepository;

    public GetRideRatingsQueryHandler(IRideRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task<Result<IReadOnlyCollection<RideRating>>> Handle(GetRideRatingsQuery query, CancellationToken cancellationToken)
    {
        var ratings = await _ratingRepository.GetForRideAsync(query.RideId, cancellationToken);
        return Result<IReadOnlyCollection<RideRating>>.Success(ratings);
    }
}
