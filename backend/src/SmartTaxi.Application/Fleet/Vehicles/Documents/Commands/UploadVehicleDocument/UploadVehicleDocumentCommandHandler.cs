using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.UploadVehicleDocument;

/// <summary>Reuses Identity.Documents' generic DocumentUploadValidator/DocumentFileNaming/IFileStorageService — none of it is User-specific.</summary>
public sealed class UploadVehicleDocumentCommandHandler : ICommandHandler<UploadVehicleDocumentCommand, Result<Guid>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string DuplicateError = "Un document identique a déjà été soumis pour ce type de document.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly DocumentUploadValidator _validator;

    public UploadVehicleDocumentCommandHandler(
        IVehicleRepository vehicleRepository, IVehicleDocumentRepository documentRepository,
        IFileStorageService fileStorage, DocumentUploadValidator validator)
    {
        _vehicleRepository = vehicleRepository;
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<Result<Guid>> Handle(UploadVehicleDocumentCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        var validation = await _validator.ValidateAndReadAsync(command.Content, command.DeclaredMimeType, cancellationToken);

        if (!validation.IsSuccess)
        {
            return Result<Guid>.Failure(validation.Error!, validation.ErrorType!.Value);
        }

        var content = validation.Value!;

        var isDuplicate = await _documentRepository.ExistsWithHashAsync(
            command.VehicleId, command.DocumentType, content.Sha256, cancellationToken);

        if (isDuplicate)
        {
            return Result<Guid>.Failure(DuplicateError, ErrorType.Conflict);
        }

        var storageKey = DocumentFileNaming.CreateStorageKey(command.VehicleId, command.DeclaredMimeType);
        await using var writeStream = new MemoryStream(content.Bytes);
        await _fileStorage.SaveAsync(storageKey, writeStream, cancellationToken);

        var document = VehicleDocument.Upload(
            command.VehicleId,
            command.DocumentType,
            storageKey,
            DocumentFileNaming.SanitizeDisplayFileName(command.OriginalFileName),
            command.DeclaredMimeType,
            content.Bytes.LongLength,
            content.Sha256,
            command.IssueDate,
            command.ExpirationDate,
            DateTime.UtcNow);

        await _documentRepository.AddAsync(document, cancellationToken);

        return Result<Guid>.Success(document.Id);
    }
}
