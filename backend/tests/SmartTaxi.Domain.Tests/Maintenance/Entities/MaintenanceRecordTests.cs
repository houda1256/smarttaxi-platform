using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Events;
using SmartTaxi.Domain.Maintenance.ValueObjects;

namespace SmartTaxi.Domain.Tests.Maintenance.Entities;

public class MaintenanceRecordTests
{
    private static readonly List<MaintenanceRecordLine> Lines =
    [
        new("Vidange", false, 1, 80m),
        new("Filtre à huile", true, 1, 20m)
    ];

    private static MaintenanceRecord CreateValid() =>
        MaintenanceRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 45000, Lines, 100m,
            "RAS", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), "6 mois pièces", DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_RaisesMaintenanceRecordCreatedAndStoresLines()
    {
        var record = CreateValid();

        var raised = Assert.Single(record.DomainEvents);
        Assert.IsType<MaintenanceRecordCreated>(raised);
        Assert.Equal(2, record.Lines.Count);
        Assert.Equal(100m, record.FinalCost);
    }

    [Fact]
    public void Create_WithEmptyMaintenanceRequestId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRecord.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, [], 0m, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNegativeFinalCost_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRecord.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, [], -1m, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNegativeMileage_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRecord.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), -1, [], 0m, null, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithNullMileage_IsAllowed_BestEffortDataOnly()
    {
        var record = MaintenanceRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, [], 0m, null, null, null,
            DateTime.UtcNow);

        Assert.Null(record.MileageAtCompletion);
    }
}

public class MaintenanceRecordLineTests
{
    [Fact]
    public void Constructor_WithValidFields_Succeeds()
    {
        var line = new MaintenanceRecordLine("Vidange", false, 1, 80m);

        Assert.Equal("Vidange", line.Description);
        Assert.False(line.IsPart);
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("Desc", 0, 10)]
    [InlineData("Desc", 1, -1)]
    public void Constructor_WithInvalidFields_Throws(string description, int quantity, decimal unitCost)
    {
        Assert.Throws<ArgumentException>(() => new MaintenanceRecordLine(description, false, quantity, unitCost));
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        var a = new MaintenanceRecordLine("Vidange", false, 1, 80m);
        var b = new MaintenanceRecordLine("Vidange", false, 1, 80m);

        Assert.Equal(a, b);
    }
}
