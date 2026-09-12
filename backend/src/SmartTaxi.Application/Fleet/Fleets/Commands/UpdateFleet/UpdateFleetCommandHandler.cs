using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.UpdateFleet;

public sealed class UpdateFleetCommandHandler : ICommandHandler<UpdateFleetCommand, Result>
{
    private const string NotFoundError = "Flotte introuvable.";

    private readonly IFleetRepository _repository;

    public UpdateFleetCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateFleetCommand command, CancellationToken cancellationToken)
    {
        var fleet = await _repository.GetByIdAsync(command.FleetId, cancellationToken);

        // Ownership mismatch and "doesn't exist" return the identical outcome
        // — never confirm another owner's fleet id.
        if (fleet is null || fleet.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        fleet.UpdateDetails(command.Name, command.Description, command.CityId, DateTime.UtcNow);
        await _repository.UpdateAsync(fleet, cancellationToken);

        return Result.Success();
    }
}
