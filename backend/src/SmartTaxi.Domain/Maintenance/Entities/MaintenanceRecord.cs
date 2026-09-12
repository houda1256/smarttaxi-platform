using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Maintenance.Events;
using SmartTaxi.Domain.Maintenance.ValueObjects;

namespace SmartTaxi.Domain.Maintenance.Entities;

/// <summary>
/// The immutable "digital maintenance book" entry — inserted exactly once
/// when a MaintenanceRequest completes (see IMaintenanceCompletionRepository).
/// No Update/Delete method exists anywhere on this entity or its repository
/// interface — a correction is deferred until genuinely needed, and would be
/// a separate compensating row referencing this one, never a mutation (same
/// philosophy as Loyalty's AdminAdjustment ledger entries). MileageAtCompletion
/// is a best-effort snapshot of Fleet's Vehicle.CurrentMileage at completion
/// time — never trusted as authoritative (the Module 9 audit found no
/// automatic mileage-update pipeline), display/history only.
/// </summary>
public sealed class MaintenanceRecord : AggregateRoot
{
    public Guid MaintenanceRequestId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid GarageUserId { get; private set; }
    public DateOnly InterventionDate { get; private set; }
    public int? MileageAtCompletion { get; private set; }

    private readonly List<MaintenanceRecordLine> _lines = [];
    public IReadOnlyCollection<MaintenanceRecordLine> Lines => _lines;

    public decimal FinalCost { get; private set; }
    public string? Notes { get; private set; }
    public DateOnly? NextRecommendedServiceDate { get; private set; }
    public string? WarrantyInfo { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private MaintenanceRecord()
    {
    }

    private MaintenanceRecord(
        Guid maintenanceRequestId, Guid vehicleId, Guid ownerUserId, Guid garageUserId, DateOnly interventionDate,
        int? mileageAtCompletion, IReadOnlyCollection<MaintenanceRecordLine> lines, decimal finalCost, string? notes,
        DateOnly? nextRecommendedServiceDate, string? warrantyInfo, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        MaintenanceRequestId = maintenanceRequestId;
        VehicleId = vehicleId;
        OwnerUserId = ownerUserId;
        GarageUserId = garageUserId;
        InterventionDate = interventionDate;
        MileageAtCompletion = mileageAtCompletion;
        _lines.AddRange(lines);
        FinalCost = finalCost;
        Notes = notes;
        NextRecommendedServiceDate = nextRecommendedServiceDate;
        WarrantyInfo = warrantyInfo;
        CreatedAtUtc = utcNow;

        RaiseDomainEvent(new MaintenanceRecordCreated(Id, maintenanceRequestId, vehicleId, utcNow));
    }

    public static MaintenanceRecord Create(
        Guid maintenanceRequestId, Guid vehicleId, Guid ownerUserId, Guid garageUserId, DateOnly interventionDate,
        int? mileageAtCompletion, IReadOnlyCollection<MaintenanceRecordLine> lines, decimal finalCost, string? notes,
        DateOnly? nextRecommendedServiceDate, string? warrantyInfo, DateTime utcNow)
    {
        if (maintenanceRequestId == Guid.Empty)
        {
            throw new ArgumentException("La demande de maintenance associée est requise.");
        }

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("Le véhicule est requis.");
        }

        if (finalCost < 0)
        {
            throw new ArgumentException("Le coût final ne peut pas être négatif.");
        }

        if (mileageAtCompletion is < 0)
        {
            throw new ArgumentException("Le kilométrage ne peut pas être négatif.");
        }

        return new MaintenanceRecord(
            maintenanceRequestId, vehicleId, ownerUserId, garageUserId, interventionDate, mileageAtCompletion, lines, finalCost,
            notes?.Trim(), nextRecommendedServiceDate, warrantyInfo?.Trim(), utcNow);
    }
}
