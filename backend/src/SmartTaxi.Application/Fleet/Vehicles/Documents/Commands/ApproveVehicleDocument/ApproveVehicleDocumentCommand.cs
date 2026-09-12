using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ApproveVehicleDocument;

public sealed record ApproveVehicleDocumentCommand(Guid ReviewerId, Guid DocumentId, string? ReviewComment) : ICommand<Result>;
