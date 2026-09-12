using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.AcknowledgeSos;

public sealed class AcknowledgeSosCommandHandler : ICommandHandler<AcknowledgeSosCommand, Result>
{
    private const string NotFoundError = "Signalement SOS introuvable ou déjà traité.";

    private readonly IRideSafetyEventRepository _safetyEventRepository;

    public AcknowledgeSosCommandHandler(IRideSafetyEventRepository safetyEventRepository)
    {
        _safetyEventRepository = safetyEventRepository;
    }

    public async Task<Result> Handle(AcknowledgeSosCommand command, CancellationToken cancellationToken)
    {
        var acknowledged = await _safetyEventRepository.TryAcknowledgeAsync(command.SafetyEventId, DateTime.UtcNow, cancellationToken);

        return acknowledged ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}
