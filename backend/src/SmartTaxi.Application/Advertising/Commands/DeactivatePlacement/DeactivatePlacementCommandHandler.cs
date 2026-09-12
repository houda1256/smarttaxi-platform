using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.DeactivatePlacement;

public sealed class DeactivatePlacementCommandHandler : ICommandHandler<DeactivatePlacementCommand, Result>
{
    private const string NotFoundError = "Emplacement publicitaire introuvable.";

    private readonly IAdvertisingPlacementRepository _repository;

    public DeactivatePlacementCommandHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeactivatePlacementCommand command, CancellationToken cancellationToken)
    {
        var placement = await _repository.GetByIdAsync(command.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        placement.Deactivate(DateTime.UtcNow);
        await _repository.UpdateAsync(placement, cancellationToken);
        return Result.Success();
    }
}
