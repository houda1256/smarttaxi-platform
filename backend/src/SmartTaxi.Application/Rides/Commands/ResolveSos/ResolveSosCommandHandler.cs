using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.ResolveSos;

public sealed class ResolveSosCommandHandler : ICommandHandler<ResolveSosCommand, Result>
{
    private const string NotFoundError = "Signalement SOS introuvable ou déjà résolu.";

    private readonly IRideSafetyEventRepository _safetyEventRepository;

    public ResolveSosCommandHandler(IRideSafetyEventRepository safetyEventRepository)
    {
        _safetyEventRepository = safetyEventRepository;
    }

    public async Task<Result> Handle(ResolveSosCommand command, CancellationToken cancellationToken)
    {
        var resolved = await _safetyEventRepository.TryResolveAsync(command.SafetyEventId, DateTime.UtcNow, cancellationToken);

        return resolved ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}
