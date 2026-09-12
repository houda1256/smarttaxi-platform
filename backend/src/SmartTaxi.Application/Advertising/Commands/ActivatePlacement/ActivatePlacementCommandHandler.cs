using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ActivatePlacement;

public sealed class ActivatePlacementCommandHandler : ICommandHandler<ActivatePlacementCommand, Result>
{
    private const string NotFoundError = "Emplacement publicitaire introuvable.";

    private readonly IAdvertisingPlacementRepository _repository;

    public ActivatePlacementCommandHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ActivatePlacementCommand command, CancellationToken cancellationToken)
    {
        var placement = await _repository.GetByIdAsync(command.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        placement.Activate(DateTime.UtcNow);
        await _repository.UpdateAsync(placement, cancellationToken);
        return Result.Success();
    }
}
