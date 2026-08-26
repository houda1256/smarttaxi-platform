using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.UpdatePlacement;

public sealed class UpdatePlacementCommandHandler : ICommandHandler<UpdatePlacementCommand, Result>
{
    private const string NotFoundError = "Emplacement publicitaire introuvable.";

    private readonly IAdvertisingPlacementRepository _repository;

    public UpdatePlacementCommandHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdatePlacementCommand command, CancellationToken cancellationToken)
    {
        var placement = await _repository.GetByIdAsync(command.PlacementId, cancellationToken);

        if (placement is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            placement.Update(command.Name, command.Description, command.SupportedMediaTypes, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.UpdateAsync(placement, cancellationToken);
        return Result.Success();
    }
}
