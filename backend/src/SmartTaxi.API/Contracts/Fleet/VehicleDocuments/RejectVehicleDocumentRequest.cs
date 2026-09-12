namespace SmartTaxi.API.Contracts.Fleet.VehicleDocuments;

public sealed record RejectVehicleDocumentRequest(string RejectionReason, string? ReviewComment);
