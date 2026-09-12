using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.SuspendVehicleDocument;

public sealed record SuspendVehicleDocumentCommand(Guid ReviewerId, Guid DocumentId, string? ReviewComment) : ICommand<Result>;
