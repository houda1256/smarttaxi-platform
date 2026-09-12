using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeVehicleDocumentAccessAuditRepository : IVehicleDocumentAccessAuditRepository
{
    private readonly List<VehicleDocumentAccessAuditEntry> _entries = [];

    public Task AddAsync(VehicleDocumentAccessAuditEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<VehicleDocumentAccessAuditEntry> Entries => _entries;
}
