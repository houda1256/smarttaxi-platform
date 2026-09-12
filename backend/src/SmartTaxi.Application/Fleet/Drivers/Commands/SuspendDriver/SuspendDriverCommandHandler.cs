using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SuspendDriver;

public sealed class SuspendDriverCommandHandler : ICommandHandler<SuspendDriverCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";
    private const string NotApprovedError = "Seul un chauffeur approuvé peut être suspendu.";
    private const string SelfApprovalError = "Un chauffeur ne peut pas examiner son propre profil.";

    private readonly IDriverProfileRepository _repository;

    public SuspendDriverCommandHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendDriverCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.DriverProfileId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (profile.UserId == command.ReviewerId)
        {
            return Result.Failure(SelfApprovalError, ErrorType.Forbidden);
        }

        if (profile.VerificationStatus != DriverVerificationStatus.Approved)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var suspended = await _repository.TrySuspendAsync(profile.Id, DateTime.UtcNow, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
