using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.CreateFleet;

public sealed class CreateFleetCommandHandler : ICommandHandler<CreateFleetCommand, Result<Guid>>
{
    private const string UserNotFoundError = "Utilisateur introuvable.";
    private const string MissingRoleError = "L'utilisateur doit avoir le rôle TaxiOwner.";

    private readonly IUserRepository _userRepository;
    private readonly IFleetRepository _repository;

    public CreateFleetCommandHandler(IUserRepository userRepository, IFleetRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateFleetCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.OwnerId, cancellationToken);

        if (user is null)
        {
            return Result<Guid>.Failure(UserNotFoundError, ErrorType.NotFound);
        }

        if (!user.HasRole(UserRole.TaxiOwner))
        {
            return Result<Guid>.Failure(MissingRoleError, ErrorType.Forbidden);
        }

        var fleet = FleetOrganization.Create(command.OwnerId, command.Name, command.Description, command.CityId, DateTime.UtcNow);
        await _repository.AddAsync(fleet, cancellationToken);

        return Result<Guid>.Success(fleet.Id);
    }
}
