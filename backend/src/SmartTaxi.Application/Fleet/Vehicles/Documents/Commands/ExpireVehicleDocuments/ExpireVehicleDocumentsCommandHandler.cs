using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ExpireVehicleDocuments;

public sealed class ExpireVehicleDocumentsCommandHandler : ICommandHandler<ExpireVehicleDocumentsCommand, Result<int>>
{
    private readonly IVehicleDocumentRepository _repository;

    public ExpireVehicleDocumentsCommandHandler(IVehicleDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(ExpireVehicleDocumentsCommand command, CancellationToken cancellationToken)
    {
        var count = await _repository.ExpireDueDocumentsAsync(DateTime.UtcNow, cancellationToken);

        return Result<int>.Success(count);
    }
}
