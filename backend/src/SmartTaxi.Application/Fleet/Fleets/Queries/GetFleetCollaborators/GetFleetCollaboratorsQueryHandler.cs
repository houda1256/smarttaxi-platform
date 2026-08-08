using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;

namespace SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetCollaborators;

public sealed class GetFleetCollaboratorsQueryHandler
    : IQueryHandler<GetFleetCollaboratorsQuery, Result<IReadOnlyCollection<FleetMemberSummary>>>
{
    private const string NotFoundError = "Flotte introuvable.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetMemberRepository _memberRepository;

    public GetFleetCollaboratorsQueryHandler(IFleetRepository fleetRepository, IFleetMemberRepository memberRepository)
    {
        _fleetRepository = fleetRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Result<IReadOnlyCollection<FleetMemberSummary>>> Handle(
        GetFleetCollaboratorsQuery query, CancellationToken cancellationToken)
    {
        var fleet = await _fleetRepository.GetByIdAsync(query.FleetId, cancellationToken);

        if (fleet is null || fleet.OwnerId != query.RequestingUserId)
        {
            return Result<IReadOnlyCollection<FleetMemberSummary>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var members = await _memberRepository.GetForFleetAsync(query.FleetId, cancellationToken);

        return Result<IReadOnlyCollection<FleetMemberSummary>>.Success(members.Select(FleetMemberSummary.FromEntity).ToList());
    }
}
