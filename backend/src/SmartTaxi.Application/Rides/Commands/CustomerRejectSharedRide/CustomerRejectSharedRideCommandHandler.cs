using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CustomerRejectSharedRide;

/// <summary>
/// If either Customer rejects, the Match ends — per documented policy, the
/// underlying individual Ride requests are never cancelled by this: each
/// Ride simply continues its own normal (unshared) lifecycle untouched.
/// </summary>
public sealed class CustomerRejectSharedRideCommandHandler : ICommandHandler<CustomerRejectSharedRideCommand, Result>
{
    private const string NotFoundError = "Proposition de trajet partagé introuvable.";
    private const string NotWaitingForApprovalError = "Cette proposition n'est plus en attente d'approbation.";

    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly ISharedRideParticipantRepository _participantRepository;

    public CustomerRejectSharedRideCommandHandler(
        ISharedRideMatchRepository matchRepository, ISharedRideParticipantRepository participantRepository)
    {
        _matchRepository = matchRepository;
        _participantRepository = participantRepository;
    }

    public async Task<Result> Handle(CustomerRejectSharedRideCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);

        if (match is null || match.Status != SharedRideMatchStatus.WaitingForCustomerApprovals)
        {
            return Result.Failure(NotWaitingForApprovalError, ErrorType.Conflict);
        }

        var participants = await _participantRepository.GetForMatchAsync(match.Id, cancellationToken);
        var mine = participants.FirstOrDefault(p => p.CustomerId == command.RequestingUserId);

        if (mine is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var rejected = await _participantRepository.TryRejectAsync(mine.Id, utcNow, cancellationToken);

        if (!rejected)
        {
            return Result.Failure(NotWaitingForApprovalError, ErrorType.Conflict);
        }

        await _matchRepository.TryRejectAsync(match.Id, utcNow, cancellationToken);

        return Result.Success();
    }
}
