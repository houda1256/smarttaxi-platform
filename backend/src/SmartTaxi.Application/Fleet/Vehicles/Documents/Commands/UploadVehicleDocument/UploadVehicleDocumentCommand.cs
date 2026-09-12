using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.UploadVehicleDocument;

public sealed record UploadVehicleDocumentCommand(
    Guid RequestingUserId,
    Guid VehicleId,
    VehicleDocumentType DocumentType,
    Stream Content,
    string OriginalFileName,
    string DeclaredMimeType,
    DateTime? IssueDate,
    DateTime? ExpirationDate) : ICommand<Result<Guid>>;
