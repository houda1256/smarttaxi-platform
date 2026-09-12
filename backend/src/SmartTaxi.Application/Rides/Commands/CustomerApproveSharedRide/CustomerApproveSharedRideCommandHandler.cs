using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.CustomerApproveSharedRide;

/// <summary>Once both paired Customers have approved, the Match moves on to WaitingForDriverApproval — the Driver still must explicitly accept separately.</summary>
public sealed class CustomerApproveSharedRideCommandHandler : ICommandHandler<CustomerApproveSharedRideCommand, Result>
{
    private const string NotFoundError = "Proposition de trajet partagé introuvable.";
    private const string NotWaitingForApprovalError = "Cette proposition n'est plus en attente d'approbation.";

    private readonly ISharedRideMatchRepository _matchRepository;
    private readonly ISharedRideParticipantRepository _participantRepository;

    public CustomerApproveSharedRideCommandHandler(
        ISharedRideMatchRepository matchRepository, ISharedRideParticipantRepository participantRepository)
    {
        _matchRepository = matchRepository;
        _participantRepository = participantRepository;
    }

    public async Task<Result> Handle(CustomerApproveSharedRideCommand command, CancellationToken cancellationToken)
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
        var approved = await _participantRepository.TryApproveAsync(mine.Id, utcNow, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotWaitingForApprovalError, ErrorType.Conflict);
        }

        var refreshed = await _participantRepository.GetForMatchAsync(match.Id, cancellationToken);

        if (refreshed.All(p => p.ApprovalStatus == SharedRideParticipantApprovalStatus.Approved))
        {
            await _matchRepository.TryMoveToWaitingForDriverApprovalAsync(match.Id, utcNow, cancellationToken);
        }

        return Result.Success();
    }
}
