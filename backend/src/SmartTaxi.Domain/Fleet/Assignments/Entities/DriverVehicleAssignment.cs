using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Assignments.Events;

namespace SmartTaxi.Domain.Fleet.Assignments.Entities;

/// <summary>
/// Status transitions (approve/activate/suspend/complete/cancel) are
/// enforced as atomic repository-level guards — the overlap-prevention rules
/// require querying across assignments and are enforced at the
/// Application/Infrastructure layer, not here.
/// </summary>
public sealed class DriverVehicleAssignment : AggregateRoot
{
    public Guid DriverId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public DaysOfWeek DaysOfWeek { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public Guid AssignedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DriverVehicleAssignment()
    {
    }

    private DriverVehicleAssignment(
        Guid driverId, Guid vehicleId, Guid ownerId, DateOnly startDate, DateOnly? endDate, TimeOnly? startTime,
        TimeOnly? endTime, DaysOfWeek daysOfWeek, Guid assignedBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        if (endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("La date de fin ne peut pas précéder la date de début.");
        }

        DriverId = driverId;
        VehicleId = vehicleId;
        OwnerId = ownerId;
        StartDate = startDate;
        EndDate = endDate;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeek = daysOfWeek;
        Status = AssignmentStatus.Draft;
        AssignedBy = assignedBy;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new DriverAssignedToVehicle(Id, driverId, vehicleId, utcNow));
    }

    public static DriverVehicleAssignment CreateDraft(
        Guid driverId, Guid vehicleId, Guid ownerId, DateOnly startDate, DateOnly? endDate, TimeOnly? startTime,
        TimeOnly? endTime, DaysOfWeek daysOfWeek, Guid assignedBy, DateTime utcNow) =>
        new(driverId, vehicleId, ownerId, startDate, endDate, startTime, endTime, daysOfWeek, assignedBy, utcNow);

    /// <summary>Two assignments "overlap" (same driver or same vehicle) if their date ranges and day-of-week masks intersect.</summary>
    public bool OverlapsWith(DriverVehicleAssignment other)
    {
        var thisEnd = EndDate ?? DateOnly.MaxValue;
        var otherEnd = other.EndDate ?? DateOnly.MaxValue;

        var dateRangesOverlap = StartDate <= otherEnd && other.StartDate <= thisEnd;
        var daysOverlap = (DaysOfWeek & other.DaysOfWeek) != DaysOfWeek.None;

        return dateRangesOverlap && daysOverlap;
    }
}
