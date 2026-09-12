using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ApproveVehicleDocument;

public sealed class ApproveVehicleDocumentCommandHandler : ICommandHandler<ApproveVehicleDocumentCommand, Result>
{
    private const string NotFoundError = "Document introuvable.";
    private const string NotPendingError = "Ce document n'est plus en attente de revue.";
    private const string SelfReviewError = "Un propriétaire ne peut pas examiner son propre document.";

    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public ApproveVehicleDocumentCommandHandler(IVehicleDocumentRepository documentRepository, IVehicleRepository vehicleRepository)
    {
        _documentRepository = documentRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result> Handle(ApproveVehicleDocumentCommand command, CancellationToken cancellationToken)
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

        if (document.Status != VehicleDocumentStatus.Pending)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        var approved = await _documentRepository.TryApproveAsync(
            document.Id, command.ReviewerId, DateTime.UtcNow, command.ReviewComment, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotPendingError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}
