using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.SuspendVehicleDocument;

public sealed class SuspendVehicleDocumentCommandHandler : ICommandHandler<SuspendVehicleDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotApprovedError = "Seul un document approuvé peut être suspendu.";
    private const string SelfReviewError = "Un propriétaire ne peut pas examiner son propre document.";

    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public SuspendVehicleDocumentCommandHandler(IVehicleDocumentRepository documentRepository, IVehicleRepository vehicleRepository)
    {
        _documentRepository = documentRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result> Handle(SuspendVehicleDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(command.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(document.VehicleId, cancellationToken);

        if (vehicle is not null && vehicle.OwnerId == command.ReviewerId)
        {
            return Result.Failure(SelfReviewError, ErrorType.Forbidden);
        }

        if (document.Status != VehicleDocumentStatus.Approved)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var suspended = await _documentRepository.TrySuspendAsync(
            document.Id, command.ReviewerId, DateTime.UtcNow, command.ReviewComment, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
