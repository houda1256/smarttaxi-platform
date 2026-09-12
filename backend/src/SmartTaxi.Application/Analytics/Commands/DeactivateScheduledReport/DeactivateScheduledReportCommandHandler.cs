using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Commands.DeactivateScheduledReport;

/// <summary>Soft-deactivation only — never a hard delete, same convention as every other module.</summary>
public sealed class DeactivateScheduledReportCommandHandler : ICommandHandler<DeactivateScheduledReportCommand, Result>
{
    private const string NotFoundError = "Rapport planifié introuvable.";

    private readonly IScheduledReportRepository _repository;

    public DeactivateScheduledReportCommandHandler(IScheduledReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeactivateScheduledReportCommand command, CancellationToken cancellationToken)
    {
        var deactivated = await _repository.TryDeactivateAsync(command.Id, DateTime.UtcNow, cancellationToken);
        return deactivated ? Result.Success() : Result.Failure(NotFoundError, ErrorType.NotFound);
    }
}
