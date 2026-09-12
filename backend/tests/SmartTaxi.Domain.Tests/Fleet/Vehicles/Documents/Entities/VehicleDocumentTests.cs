using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Domain.Tests.Fleet.Vehicles.Documents.Entities;

public class VehicleDocumentTests
{
    [Fact]
    public void Upload_SetsInitialStateAsFirstPendingVersion()
    {
        var vehicleId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var document = VehicleDocument.Upload(
            vehicleId, VehicleDocumentType.Insurance, "key", "insurance.pdf", "application/pdf",
            1024, "hash", null, null, utcNow);

        Assert.Equal(vehicleId, document.VehicleId);
        Assert.Equal(VehicleDocumentStatus.Pending, document.Status);
        Assert.Equal(1, document.Version);
        Assert.Null(document.ReplacesDocumentId);
    }

    [Fact]
    public void CreateReplacement_IncrementsVersionAndLinksToOriginal()
    {
        var original = VehicleDocument.Upload(
            Guid.NewGuid(), VehicleDocumentType.Insurance, "key-1", "a.pdf", "application/pdf",
            1, "hash-1", null, null, DateTime.UtcNow);

        var replacement = original.CreateReplacement("key-2", "b.pdf", "application/pdf", 2, "hash-2", null, null, DateTime.UtcNow);

        Assert.Equal(2, replacement.Version);
        Assert.Equal(original.Id, replacement.ReplacesDocumentId);
        Assert.Equal(VehicleDocumentStatus.Pending, replacement.Status);
    }

    [Fact]
    public void IsCurrentlyValid_ForApprovedNotExpired_ReturnsTrue()
    {
        var document = VehicleDocument.Upload(
            Guid.NewGuid(), VehicleDocumentType.TechnicalInspection, "key", "f.pdf", "application/pdf",
            1, "hash", null, DateTime.UtcNow.AddDays(30), DateTime.UtcNow);
        typeof(VehicleDocument).GetProperty(nameof(VehicleDocument.Status))!.SetValue(document, VehicleDocumentStatus.Approved);

        Assert.True(document.IsCurrentlyValid(DateTime.UtcNow));
    }

    [Fact]
    public void IsCurrentlyValid_ForExpiredApproved_ReturnsFalse()
    {
        var document = VehicleDocument.Upload(
            Guid.NewGuid(), VehicleDocumentType.TechnicalInspection, "key", "f.pdf", "application/pdf",
            1, "hash", null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-30));
        typeof(VehicleDocument).GetProperty(nameof(VehicleDocument.Status))!.SetValue(document, VehicleDocumentStatus.Approved);

        Assert.False(document.IsCurrentlyValid(DateTime.UtcNow));
    }
}
