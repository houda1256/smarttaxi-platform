using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetById;

/// <summary>Visible to the owning user or to any collaborator explicitly added to this fleet — never to unrelated users.</summary>
public sealed class GetFleetByIdQueryHandler : IQueryHandler<GetFleetByIdQuery, Result<FleetSummary>>
{
    private const string NotFoundError = "Flotte introuvable.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetMemberRepository _memberRepository;

    public GetFleetByIdQueryHandler(IFleetRepository fleetRepository, IFleetMemberRepository memberRepository)
    {
        _fleetRepository = fleetRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Result<FleetSummary>> Handle(GetFleetByIdQuery query, CancellationToken cancellationToken)
    {
        var fleet = await _fleetRepository.GetByIdAsync(query.FleetId, cancellationToken);

        if (fleet is null)
        {
            return Result<FleetSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (fleet.OwnerId != query.RequestingUserId)
        {
            var membership = await _memberRepository.GetMembershipAsync(query.FleetId, query.RequestingUserId, cancellationToken);

            if (membership is null)
            {
                return Result<FleetSummary>.Failure(NotFoundError, ErrorType.NotFound);
            }
        }

        return Result<FleetSummary>.Success(FleetSummary.FromEntity(fleet));
    }
}
