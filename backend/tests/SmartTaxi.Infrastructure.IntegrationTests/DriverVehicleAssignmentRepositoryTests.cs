using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class DriverVehicleAssignmentRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public DriverVehicleAssignmentRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static DriverVehicleAssignment NewPendingApprovalAssignment(Guid driverId, Guid vehicleId, Guid ownerId)
    {
        var assignment = DriverVehicleAssignment.CreateDraft(
            driverId, vehicleId, ownerId, new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All, ownerId, DateTime.UtcNow);

        typeof(DriverVehicleAssignment).GetProperty(nameof(DriverVehicleAssignment.Status))!
            .SetValue(assignment, AssignmentStatus.PendingApproval);

        return assignment;
    }

    [Fact]
    public async Task ConcurrentTryActivateAsync_OnTheSamePendingApprovalAssignment_OnlyOneAttemptSucceeds()
    {
        var assignment = NewPendingApprovalAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new DriverVehicleAssignmentRepository(writeContext).AddAsync(assignment, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new DriverVehicleAssignmentRepository(contextA);
        var repositoryB = new DriverVehicleAssignmentRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryActivateAsync(assignment.Id, utcNow, CancellationToken.None),
            repositoryB.TryActivateAsync(assignment.Id, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new DriverVehicleAssignmentRepository(readContext).GetByIdAsync(assignment.Id, CancellationToken.None);
        Assert.Equal(AssignmentStatus.Active, finalState!.Status);
    }
}
