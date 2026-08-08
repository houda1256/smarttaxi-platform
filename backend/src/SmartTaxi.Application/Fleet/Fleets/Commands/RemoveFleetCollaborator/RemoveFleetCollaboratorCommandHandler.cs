using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.RemoveFleetCollaborator;

public sealed class RemoveFleetCollaboratorCommandHandler : ICommandHandler<RemoveFleetCollaboratorCommand, Result>
{
    private const string NotFoundError = "Flotte ou collaborateur introuvable.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetMemberRepository _memberRepository;

    public RemoveFleetCollaboratorCommandHandler(IFleetRepository fleetRepository, IFleetMemberRepository memberRepository)
    {
        _fleetRepository = fleetRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Result> Handle(RemoveFleetCollaboratorCommand command, CancellationToken cancellationToken)
    {
        var fleet = await _fleetRepository.GetByIdAsync(command.FleetId, cancellationToken);

        if (fleet is null || fleet.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var member = await _memberRepository.GetByIdAsync(command.MemberId, cancellationToken);

        if (member is null || member.FleetId != command.FleetId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        await _memberRepository.RemoveAsync(member.Id, cancellationToken);

        return Result.Success();
    }
}
