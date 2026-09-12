using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ReplaceVehicleDocument;

public sealed record ReplaceVehicleDocumentCommand(
    Guid RequestingUserId,
    Guid DocumentId,
    Stream Content,
    string OriginalFileName,
    string DeclaredMimeType,
    DateTime? IssueDate,
    DateTime? ExpirationDate) : ICommand<Result<Guid>>;
