using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class VehicleRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public VehicleRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Vehicle NewVehicle(Guid ownerId, string plate, string? vin = null) => Vehicle.Register(
        ownerId, null, "Toyota", "Corolla", 2022, "White", plate, vin, 0,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

    [Fact]
    public async Task GetForOwnerAsync_ReturnsOnlyThatOwnersVehicles()
    {
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new VehicleRepository(writeContext);
            await repository.AddAsync(NewVehicle(ownerA, $"AA-{Guid.NewGuid():N}"[..8]), CancellationToken.None);
            await repository.AddAsync(NewVehicle(ownerA, $"AB-{Guid.NewGuid():N}"[..8]), CancellationToken.None);
            await repository.AddAsync(NewVehicle(ownerB, $"AC-{Guid.NewGuid():N}"[..8]), CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var vehiclesForA = await new VehicleRepository(readContext).GetForOwnerAsync(ownerA, CancellationToken.None);

        Assert.Equal(2, vehiclesForA.Count);
        Assert.All(vehiclesForA, v => Assert.Equal(ownerA, v.OwnerId));
    }

    [Fact]
    public async Task ConcurrentAddAsync_WithSameLicensePlate_OnlyOneSucceeds_EnforcedByDbUniqueIndex()
    {
        var sharedPlate = $"PL-{Guid.NewGuid():N}"[..10];
        var vehicleA = NewVehicle(Guid.NewGuid(), sharedPlate);
        var vehicleB = NewVehicle(Guid.NewGuid(), sharedPlate);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            TryAddAsync(new VehicleRepository(contextA), vehicleA),
            TryAddAsync(new VehicleRepository(contextB), vehicleB));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.Vehicles.CountAsync(v => v.LicensePlate == sharedPlate);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConcurrentApproveAndReject_OnTheSamePendingVehicle_OnlyOneAttemptSucceeds()
    {
        var vehicle = NewVehicle(Guid.NewGuid(), $"RJ-{Guid.NewGuid():N}"[..10]);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new VehicleRepository(writeContext).AddAsync(vehicle, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new VehicleRepository(contextA);
        var repositoryB = new VehicleRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryApproveAsync(vehicle.Id, Guid.NewGuid(), utcNow, CancellationToken.None),
            repositoryB.TryRejectAsync(vehicle.Id, Guid.NewGuid(), utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.True(finalState!.VerificationStatus is VehicleVerificationStatus.Approved or VehicleVerificationStatus.Rejected);
    }

    private static async Task<bool> TryAddAsync(VehicleRepository repository, Vehicle vehicle)
    {
        try
        {
            await repository.AddAsync(vehicle, CancellationToken.None);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return false;
        }
    }
}
