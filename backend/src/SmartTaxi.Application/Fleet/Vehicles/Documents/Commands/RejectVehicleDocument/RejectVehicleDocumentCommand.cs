using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.RejectVehicleDocument;

public sealed record RejectVehicleDocumentCommand(
    Guid ReviewerId, Guid DocumentId, string RejectionReason, string? ReviewComment) : ICommand<Result>;
