using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.RejectSharedRideByDriver;

/// <summary>The paired Rides are never touched — per the documented policy, they remain available for normal (unshared) driver selection.</summary>
public sealed class RejectSharedRideByDriverCommandHandler : ICommandHandler<RejectSharedRideByDriverCommand, Result>
{
    private const string NotWaitingForDriverError = "Cette proposition n'est pas en attente d'approbation du chauffeur.";
    private const string DriverNotFoundError = "Profil chauffeur introuvable.";

    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public RejectSharedRideByDriverCommandHandler(ISharedRideMatchRepository matchRepository, IDriverProfileRepository driverRepository)
    {
        _matchRepository = matchRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(RejectSharedRideByDriverCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);

        if (match is null || match.Status != SharedRideMatchStatus.WaitingForDriverApproval)
        {
            return Result.Failure(NotWaitingForDriverError, ErrorType.Conflict);
        }

        var driver = await _driverRepository.GetByUserIdAsync(command.RequestingUserId, cancellationToken);

        if (driver is null)
        {
            return Result.Failure(DriverNotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var rejected = await _matchRepository.TryRejectAsync(match.Id, utcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotWaitingForDriverError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
