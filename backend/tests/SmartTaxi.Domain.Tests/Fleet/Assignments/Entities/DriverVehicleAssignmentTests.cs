using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Assignments.Events;

namespace SmartTaxi.Domain.Tests.Fleet.Assignments.Entities;

public class DriverVehicleAssignmentTests
{
    [Fact]
    public void CreateDraft_SetsDraftStatusAndRaisesEvent()
    {
        var driverId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var assignment = DriverVehicleAssignment.CreateDraft(
            driverId, vehicleId, Guid.NewGuid(), new DateOnly(2026, 1, 1), null, null, null,
            DaysOfWeek.All, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(AssignmentStatus.Draft, assignment.Status);
        Assert.Single(assignment.DomainEvents);
        var raised = Assert.IsType<DriverAssignedToVehicle>(assignment.DomainEvents.Single());
        Assert.Equal(driverId, raised.DriverId);
        Assert.Equal(vehicleId, raised.VehicleId);
    }

    [Fact]
    public void CreateDraft_WithEndDateBeforeStartDate_Throws()
    {
        Assert.Throws<ArgumentException>(() => DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1),
            null, null, DaysOfWeek.All, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void OverlapsWith_ForOverlappingDateRangesAndSharedDays_ReturnsTrue()
    {
        var a = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            null, null, DaysOfWeek.Monday | DaysOfWeek.Wednesday, Guid.NewGuid(), DateTime.UtcNow);
        var b = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15),
            null, null, DaysOfWeek.Wednesday | DaysOfWeek.Friday, Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_ForNonOverlappingDateRanges_ReturnsFalse()
    {
        var a = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10),
            null, null, DaysOfWeek.All, Guid.NewGuid(), DateTime.UtcNow);
        var b = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 2, 1), null,
            null, null, DaysOfWeek.All, Guid.NewGuid(), DateTime.UtcNow);

        Assert.False(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_ForOverlappingDatesButDisjointDays_ReturnsFalse()
    {
        var a = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            null, null, DaysOfWeek.Monday, Guid.NewGuid(), DateTime.UtcNow);
        var b = DriverVehicleAssignment.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            null, null, DaysOfWeek.Tuesday, Guid.NewGuid(), DateTime.UtcNow);

        Assert.False(a.OverlapsWith(b));
    }
}
