using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Commands.CreatePlacement;

public sealed class CreatePlacementCommandHandler : ICommandHandler<CreatePlacementCommand, Result<Guid>>
{
    private const string DuplicateCodeError = "Un emplacement avec ce code existe déjà.";

    private readonly IAdvertisingPlacementRepository _repository;

    public CreatePlacementCommandHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreatePlacementCommand command, CancellationToken cancellationToken)
    {
        if (await _repository.GetByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
        }

        AdvertisingPlacement placement;

        try
        {
            placement = AdvertisingPlacement.Create(command.Code, command.Name, command.Description, command.SupportedMediaTypes, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _repository.AddAsync(placement, cancellationToken);
        return Result<Guid>.Success(placement.Id);
    }
}
