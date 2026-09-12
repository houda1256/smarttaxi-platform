using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideSafetyEvents;

/// <summary>Admin/SecurityOfficer read — gated by the rides.monitor permission at the API layer, no ownership check here.</summary>
public sealed class GetRideSafetyEventsQueryHandler : IQueryHandler<GetRideSafetyEventsQuery, Result<IReadOnlyCollection<RideSafetyEvent>>>
{
    private readonly IRideSafetyEventRepository _safetyEventRepository;

    public GetRideSafetyEventsQueryHandler(IRideSafetyEventRepository safetyEventRepository)
    {
        _safetyEventRepository = safetyEventRepository;
    }

    public async Task<Result<IReadOnlyCollection<RideSafetyEvent>>> Handle(GetRideSafetyEventsQuery query, CancellationToken cancellationToken)
    {
        var events = await _safetyEventRepository.GetForRideAsync(query.RideId, cancellationToken);
        return Result<IReadOnlyCollection<RideSafetyEvent>>.Success(events);
    }
}
