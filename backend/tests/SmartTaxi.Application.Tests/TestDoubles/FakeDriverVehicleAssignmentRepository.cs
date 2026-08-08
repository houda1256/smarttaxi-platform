using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverVehicleAssignmentRepository : IDriverVehicleAssignmentRepository
{
    private readonly Dictionary<Guid, DriverVehicleAssignment> _assignmentsById = new();

    private static readonly AssignmentStatus[] NonTerminalStatuses =
        [AssignmentStatus.Draft, AssignmentStatus.PendingApproval, AssignmentStatus.Active];

    public Task AddAsync(DriverVehicleAssignment assignment, CancellationToken cancellationToken)
    {
        _assignmentsById[assignment.Id] = assignment;
        return Task.CompletedTask;
    }

    public Task<DriverVehicleAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        Task.FromResult(_assignmentsById.GetValueOrDefault(assignmentId));

    public Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverVehicleAssignment> assignments =
            _assignmentsById.Values.Where(a => a.DriverId == driverId).ToList();
        return Task.FromResult(assignments);
    }

    public Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverVehicleAssignment> assignments =
            _assignmentsById.Values.Where(a => a.VehicleId == vehicleId).ToList();
        return Task.FromResult(assignments);
    }

    public Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForDriverAsync(
        Guid driverId, Guid? excludeAssignmentId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverVehicleAssignment> assignments = _assignmentsById.Values
            .Where(a => a.DriverId == driverId && a.Id != excludeAssignmentId && NonTerminalStatuses.Contains(a.Status))
            .ToList();
        return Task.FromResult(assignments);
    }

    public Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForVehicleAsync(
        Guid vehicleId, Guid? excludeAssignmentId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverVehicleAssignment> assignments = _assignmentsById.Values
            .Where(a => a.VehicleId == vehicleId && a.Id != excludeAssignmentId && NonTerminalStatuses.Contains(a.Status))
            .ToList();
        return Task.FromResult(assignments);
    }

    public Task<bool> TryApproveAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(assignmentId, AssignmentStatus.Draft, AssignmentStatus.PendingApproval, utcNow);

    public Task<bool> TryActivateAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(assignmentId, AssignmentStatus.PendingApproval, AssignmentStatus.Active, utcNow);

    public Task<bool> TrySuspendAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(assignmentId, AssignmentStatus.Active, AssignmentStatus.Suspended, utcNow);

    public Task<bool> TryCompleteAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(assignmentId, AssignmentStatus.Active, AssignmentStatus.Completed, utcNow);

    public Task<bool> TryCancelAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_assignmentsById.TryGetValue(assignmentId, out var assignment)
            || assignment.Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled)
        {
            return Task.FromResult(false);
        }

        SetProperty(assignment, nameof(DriverVehicleAssignment.Status), AssignmentStatus.Cancelled);
        SetProperty(assignment, nameof(DriverVehicleAssignment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransition(Guid assignmentId, AssignmentStatus from, AssignmentStatus to, DateTime utcNow)
    {
        if (!_assignmentsById.TryGetValue(assignmentId, out var assignment) || assignment.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(assignment, nameof(DriverVehicleAssignment.Status), to);
        SetProperty(assignment, nameof(DriverVehicleAssignment.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(DriverVehicleAssignment assignment, string propertyName, object? value) =>
        typeof(DriverVehicleAssignment).GetProperty(propertyName)!.SetValue(assignment, value);
}
