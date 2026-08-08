using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ExpireVehicleDocuments;

public sealed record ExpireVehicleDocumentsCommand : ICommand<Result<int>>;
