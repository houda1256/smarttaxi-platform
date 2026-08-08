using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ReplaceVehicleDocument;

public sealed class ReplaceVehicleDocumentCommandHandler : ICommandHandler<ReplaceVehicleDocumentCommand, Result<Guid>>
{
    private const string NotFoundError = "Document introuvable.";
    private const string AlreadyReplacedError = "Ce document a déjà été remplacé par une version plus récente.";
    private const string DuplicateError = "Un document identique a déjà été soumis pour ce type de document.";
    private const string ConcurrentReplaceError = "Ce document vient d'être remplacé par une autre requête.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly DocumentUploadValidator _validator;

    public ReplaceVehicleDocumentCommandHandler(
        IVehicleRepository vehicleRepository, IVehicleDocumentRepository documentRepository,
        IFileStorageService fileStorage, DocumentUploadValidator validator)
    {
        _vehicleRepository = vehicleRepository;
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<Result<Guid>> Handle(ReplaceVehicleDocumentCommand command, CancellationToken cancellationToken)
    {
        var current = await _documentRepository.GetByIdAsync(command.DocumentId, cancellationToken);

        if (current is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(current.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (current.Status == VehicleDocumentStatus.Replaced)
        {
            return Result<Guid>.Failure(AlreadyReplacedError, ErrorType.Conflict);
        }

        var validation = await _validator.ValidateAndReadAsync(command.Content, command.DeclaredMimeType, cancellationToken);

        if (!validation.IsSuccess)
        {
            return Result<Guid>.Failure(validation.Error!, validation.ErrorType!.Value);
        }

        var content = validation.Value!;

        var isDuplicate = await _documentRepository.ExistsWithHashAsync(
            current.VehicleId, current.DocumentType, content.Sha256, cancellationToken);

        if (isDuplicate)
        {
            return Result<Guid>.Failure(DuplicateError, ErrorType.Conflict);
        }

        var storageKey = DocumentFileNaming.CreateStorageKey(current.VehicleId, command.DeclaredMimeType);
        await using var writeStream = new MemoryStream(content.Bytes);
        await _fileStorage.SaveAsync(storageKey, writeStream, cancellationToken);

        var utcNow = DateTime.UtcNow;
        var replacement = current.CreateReplacement(
            storageKey,
            DocumentFileNaming.SanitizeDisplayFileName(command.OriginalFileName),
            command.DeclaredMimeType,
            content.Bytes.LongLength,
            content.Sha256,
            command.IssueDate,
            command.ExpirationDate,
            utcNow);

        var replaced = await _documentRepository.TryReplaceAsync(current, replacement, utcNow, cancellationToken);

        if (!replaced)
        {
            return Result<Guid>.Failure(ConcurrentReplaceError, ErrorType.Conflict);
        }

        return Result<Guid>.Success(replacement.Id);
    }
}
