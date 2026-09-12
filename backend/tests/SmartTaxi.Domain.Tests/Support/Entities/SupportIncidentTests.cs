using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Domain.Support.Events;

namespace SmartTaxi.Domain.Tests.Support.Entities;

public class SupportIncidentTests
{
    private static SupportIncident CreateValid() =>
        SupportIncident.Create(
            SupportIncidentType.SafetyIncident, SupportIncidentSeverity.Critical, "SOS déclenché", "Le passager a activé le SOS.",
            Guid.NewGuid(), SupportRelatedEntityType.Ride, Guid.NewGuid(), 36.8, 10.18, DateTime.UtcNow, "RideSafetyEvent", Guid.NewGuid(),
            DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_StartsAtReportedAndRaisesEvent()
    {
        var incident = CreateValid();

        Assert.Equal(SupportIncidentStatus.Reported, incident.Status);
        var raised = Assert.Single(incident.DomainEvents);
        Assert.IsType<SupportIncidentReported>(raised);
        Assert.StartsWith("INC-", incident.IncidentNumber);
        Assert.Null(incident.AssignedAdminUserId);
    }

    [Fact]
    public void Create_WithoutSourceOrLocation_Succeeds()
    {
        var incident = SupportIncident.Create(
            SupportIncidentType.TechnicalIncident, SupportIncidentSeverity.Minor, "Erreur applicative", "Description", null, null, null, null,
            null, DateTime.UtcNow, null, null, DateTime.UtcNow);

        Assert.Null(incident.SourceType);
        Assert.Null(incident.Latitude);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            SupportIncident.Create(
                SupportIncidentType.OperationalIncident, SupportIncidentSeverity.Moderate, title, "Description", null, null, null, null,
                null, DateTime.UtcNow, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithOnlyLatitudeProvided_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportIncident.Create(
                SupportIncidentType.VehicleIncident, SupportIncidentSeverity.Major, "Titre", "Description", null, null, null, 36.8, null,
                DateTime.UtcNow, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithOutOfRangeLatitude_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportIncident.Create(
                SupportIncidentType.VehicleIncident, SupportIncidentSeverity.Major, "Titre", "Description", null, null, null, 200, 10.18,
                DateTime.UtcNow, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithSourceTypeButNoSourceId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportIncident.Create(
                SupportIncidentType.FraudIncident, SupportIncidentSeverity.Critical, "Titre", "Description", null, null, null, null, null,
                DateTime.UtcNow, "SupportTicket", null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithRelatedEntityIdButNoType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SupportIncident.Create(
                SupportIncidentType.RideIncident, SupportIncidentSeverity.Moderate, "Titre", "Description", null, null, Guid.NewGuid(), null,
                null, DateTime.UtcNow, null, null, DateTime.UtcNow));
    }
}
