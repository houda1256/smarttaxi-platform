using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.AddFleetCollaborator;

public sealed class AddFleetCollaboratorCommandHandler : ICommandHandler<AddFleetCollaboratorCommand, Result<Guid>>
{
    private const string NotFoundError = "Flotte introuvable.";
    private const string AlreadyMemberError = "Cet utilisateur est déjà collaborateur de cette flotte.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetMemberRepository _memberRepository;

    public AddFleetCollaboratorCommandHandler(IFleetRepository fleetRepository, IFleetMemberRepository memberRepository)
    {
        _fleetRepository = fleetRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Result<Guid>> Handle(AddFleetCollaboratorCommand command, CancellationToken cancellationToken)
    {
        var fleet = await _fleetRepository.GetByIdAsync(command.FleetId, cancellationToken);

        if (fleet is null || fleet.OwnerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var existingMembership = await _memberRepository.GetMembershipAsync(command.FleetId, command.CollaboratorUserId, cancellationToken);

        if (existingMembership is not null)
        {
            return Result<Guid>.Failure(AlreadyMemberError, ErrorType.Conflict);
        }

        var member = new FleetMember(command.FleetId, command.CollaboratorUserId, command.Role, DateTime.UtcNow);
        await _memberRepository.AddAsync(member, cancellationToken);

        return Result<Guid>.Success(member.Id);
    }
}
