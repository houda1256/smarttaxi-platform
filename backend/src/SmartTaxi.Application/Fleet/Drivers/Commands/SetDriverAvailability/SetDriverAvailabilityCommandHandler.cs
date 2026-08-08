using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SetDriverAvailability;

public sealed class SetDriverAvailabilityCommandHandler : ICommandHandler<SetDriverAvailabilityCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";

    private readonly IDriverProfileRepository _repository;

    public SetDriverAvailabilityCommandHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SetDriverAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.DriverProfileId, cancellationToken);

        if (profile is null || profile.UserId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            profile.SetAvailability(command.Status, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.UpdateAsync(profile, cancellationToken);

        return Result.Success();
    }
}
