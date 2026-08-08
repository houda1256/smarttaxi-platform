using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.SuspendFleet;

public sealed class SuspendFleetCommandHandler : ICommandHandler<SuspendFleetCommand, Result>
{
    private const string NotFoundError = "Flotte introuvable.";
    private const string NotActiveError = "Seule une flotte active peut être suspendue.";

    private readonly IFleetRepository _repository;

    public SuspendFleetCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendFleetCommand command, CancellationToken cancellationToken)
    {
        var fleet = await _repository.GetByIdAsync(command.FleetId, cancellationToken);

        if (fleet is null || fleet.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (fleet.Status != FleetStatus.Active)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        var suspended = await _repository.TrySuspendAsync(fleet.Id, DateTime.UtcNow, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
