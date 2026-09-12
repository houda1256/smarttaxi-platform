using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class DriverVehicleAssignmentRepository : IDriverVehicleAssignmentRepository
{
    private static readonly AssignmentStatus[] NonTerminalStatuses =
        [AssignmentStatus.Draft, AssignmentStatus.PendingApproval, AssignmentStatus.Active];

    private readonly ApplicationDbContext _context;

    public DriverVehicleAssignmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DriverVehicleAssignment assignment, CancellationToken cancellationToken)
    {
        await _context.DriverVehicleAssignments.AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<DriverVehicleAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        _context.DriverVehicleAssignments.FirstOrDefaultAsync(assignment => assignment.Id == assignmentId, cancellationToken);

    public async Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForDriverAsync(
        Guid driverId, CancellationToken cancellationToken) =>
        await _context.DriverVehicleAssignments.Where(assignment => assignment.DriverId == driverId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DriverVehicleAssignment>> GetForVehicleAsync(
        Guid vehicleId, CancellationToken cancellationToken) =>
        await _context.DriverVehicleAssignments.Where(assignment => assignment.VehicleId == vehicleId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForDriverAsync(
        Guid driverId, Guid? excludeAssignmentId, CancellationToken cancellationToken) =>
        await _context.DriverVehicleAssignments
            .Where(assignment => assignment.DriverId == driverId && assignment.Id != excludeAssignmentId
                && NonTerminalStatuses.Contains(assignment.Status))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DriverVehicleAssignment>> GetActiveOrPendingForVehicleAsync(
        Guid vehicleId, Guid? excludeAssignmentId, CancellationToken cancellationToken) =>
        await _context.DriverVehicleAssignments
            .Where(assignment => assignment.VehicleId == vehicleId && assignment.Id != excludeAssignmentId
                && NonTerminalStatuses.Contains(assignment.Status))
            .ToListAsync(cancellationToken);

    public Task<bool> TryApproveAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(assignmentId, AssignmentStatus.Draft, AssignmentStatus.PendingApproval, utcNow, cancellationToken);

    public Task<bool> TryActivateAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(assignmentId, AssignmentStatus.PendingApproval, AssignmentStatus.Active, utcNow, cancellationToken);

    public Task<bool> TrySuspendAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(assignmentId, AssignmentStatus.Active, AssignmentStatus.Suspended, utcNow, cancellationToken);

    public Task<bool> TryCompleteAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(assignmentId, AssignmentStatus.Active, AssignmentStatus.Completed, utcNow, cancellationToken);

    public async Task<bool> TryCancelAsync(Guid assignmentId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverVehicleAssignments
            .Where(assignment => assignment.Id == assignmentId
                && assignment.Status != AssignmentStatus.Completed && assignment.Status != AssignmentStatus.Cancelled)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(assignment => assignment.Status, AssignmentStatus.Cancelled)
                .SetProperty(assignment => assignment.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    private async Task<bool> TryTransitionAsync(
        Guid assignmentId, AssignmentStatus from, AssignmentStatus to, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverVehicleAssignments
            .Where(assignment => assignment.Id == assignmentId && assignment.Status == from)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(assignment => assignment.Status, to)
                .SetProperty(assignment => assignment.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
