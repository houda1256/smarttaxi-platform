using SmartTaxi.Application.Fleet.Vehicles;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleEligibility;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Vehicles.Queries;

public class GetVehicleEligibilityQueryHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeVehicleDocumentRepository _documentRepository = new();
    private readonly GetVehicleEligibilityQueryHandler _handler;

    public GetVehicleEligibilityQueryHandlerTests()
    {
        _handler = new GetVehicleEligibilityQueryHandler(_vehicleRepository, new VehicleEligibilityChecker(_documentRepository));
    }

    private static Vehicle RegisterVehicle() => Vehicle.Register(
        Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

    private async Task ApproveAllCriticalDocumentsAsync(Guid vehicleId, IReadOnlyCollection<VehicleDocumentType>? skip = null)
    {
        foreach (var type in Domain.Fleet.Vehicles.Documents.Policies.VehicleDocumentRequirements.Critical)
        {
            if (skip is not null && skip.Contains(type))
            {
                continue;
            }

            var document = VehicleDocument.Upload(vehicleId, type, "key", "f.pdf", "application/pdf", 1, $"hash-{type}", null, null, DateTime.UtcNow);
            await _documentRepository.AddAsync(document, CancellationToken.None);
            await _documentRepository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Handle_ForUnapprovedVehicle_ReturnsNotEligibleEvenWithAllDocumentsApproved()
    {
        var vehicle = RegisterVehicle();
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await ApproveAllCriticalDocumentsAsync(vehicle.Id);

        var result = await _handler.Handle(new GetVehicleEligibilityQuery(vehicle.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsEligible);
    }

    [Fact]
    public async Task Handle_ForApprovedVehicleWithAllDocumentsValid_ReturnsEligible()
    {
        var vehicle = RegisterVehicle();
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        await ApproveAllCriticalDocumentsAsync(vehicle.Id);

        var result = await _handler.Handle(new GetVehicleEligibilityQuery(vehicle.Id), CancellationToken.None);

        Assert.True(result.Value!.IsEligible);
        Assert.Empty(result.Value.MissingOrInvalidDocumentTypes);
    }

    [Fact]
    public async Task Handle_ForApprovedVehicleWithExpiredCriticalDocument_ReturnsNotEligible()
    {
        var vehicle = RegisterVehicle();
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        await ApproveAllCriticalDocumentsAsync(vehicle.Id, skip: [VehicleDocumentType.Insurance]);

        var expiredInsurance = VehicleDocument.Upload(
            vehicle.Id, VehicleDocumentType.Insurance, "key2", "f2.pdf", "application/pdf", 1, "hash-expired",
            null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-30));
        await _documentRepository.AddAsync(expiredInsurance, CancellationToken.None);
        await _documentRepository.TryApproveAsync(expiredInsurance.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), null, CancellationToken.None);

        var result = await _handler.Handle(new GetVehicleEligibilityQuery(vehicle.Id), CancellationToken.None);

        Assert.False(result.Value!.IsEligible);
        Assert.Contains(VehicleDocumentType.Insurance, result.Value.MissingOrInvalidDocumentTypes);
    }
}
