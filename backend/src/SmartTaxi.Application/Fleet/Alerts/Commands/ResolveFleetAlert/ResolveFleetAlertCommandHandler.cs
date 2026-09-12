using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Alerts.Abstractions;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Application.Fleet.Alerts.Commands.ResolveFleetAlert;

public sealed class ResolveFleetAlertCommandHandler : ICommandHandler<ResolveFleetAlertCommand, Result>
{
    private const string NotFoundError = "Alerte introuvable.";
    private const string NotOpenError = "Cette alerte n'est pas ouverte.";

    private readonly IFleetAlertRepository _repository;

    public ResolveFleetAlertCommandHandler(IFleetAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ResolveFleetAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await _repository.GetByIdAsync(command.AlertId, cancellationToken);

        if (alert is null || alert.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (alert.Status != FleetAlertStatus.Open)
        {
            return Result.Failure(NotOpenError, ErrorType.Conflict);
        }

        var resolved = await _repository.TryResolveAsync(alert.Id, DateTime.UtcNow, cancellationToken);

        if (!resolved)
        {
            return Result.Failure(NotOpenError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
