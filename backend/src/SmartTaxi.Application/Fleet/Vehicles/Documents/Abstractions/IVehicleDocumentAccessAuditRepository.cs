using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

namespace SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;

public interface IVehicleDocumentAccessAuditRepository
{
    Task AddAsync(VehicleDocumentAccessAuditEntry entry, CancellationToken cancellationToken);
}
