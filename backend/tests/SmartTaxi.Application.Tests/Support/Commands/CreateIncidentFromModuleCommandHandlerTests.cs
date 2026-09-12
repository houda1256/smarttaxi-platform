using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromFinancialDispute;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromMaintenance;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRide;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRoadside;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class CreateIncidentFromModuleCommandHandlerTests
{
    private readonly FakeSupportIncidentReporter _incidentReporter = new();

    [Fact]
    public async Task FromRide_UnknownRide_ReturnsNotFound()
    {
        var handler = new CreateIncidentFromRideCommandHandler(new FakeRideRepository(), _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromRideCommand(Guid.NewGuid(), Guid.NewGuid(), SupportIncidentType.RideIncident, SupportIncidentSeverity.Minor, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(_incidentReporter.Requests);
    }

    [Fact]
    public async Task FromRide_ExistingRide_ReportsIncidentWithSourceAnchor()
    {
        var rideRepository = new FakeRideRepository();
        var ride = Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "A", GeoCoordinate.Create(36.8, 10.18), "B", GeoCoordinate.Create(36.9, 10.2), null, 1, 0,
            false, false, false, false, null, RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await rideRepository.AddAsync(ride, CancellationToken.None);
        var handler = new CreateIncidentFromRideCommandHandler(rideRepository, _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromRideCommand(ride.Id, Guid.NewGuid(), SupportIncidentType.RideIncident, SupportIncidentSeverity.Major, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var request = Assert.Single(_incidentReporter.Requests);
        Assert.Equal("Ride", request.SourceType);
        Assert.Equal(ride.Id, request.SourceId);
        Assert.Equal(SupportRelatedEntityType.Ride, request.RelatedEntityType);
    }

    [Fact]
    public async Task FromFinancialDispute_UnknownDispute_ReturnsNotFound()
    {
        var accountRepository = new FakeFinancialAccountRepository();
        var ledgerRepository = new FakeFinancialLedgerRepository(accountRepository);
        var payoutRepository = new FakePayoutRepository(accountRepository, ledgerRepository);
        var disputeRepository = new FakeFinancialDisputeRepository(accountRepository, payoutRepository);
        var handler = new CreateIncidentFromFinancialDisputeCommandHandler(disputeRepository, _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromFinancialDisputeCommand(Guid.NewGuid(), Guid.NewGuid(), SupportIncidentSeverity.Critical),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task FromFinancialDispute_ExistingDispute_ReportsIncidentWithSourceAnchor()
    {
        var accountRepository = new FakeFinancialAccountRepository();
        var ledgerRepository = new FakeFinancialLedgerRepository(accountRepository);
        var payoutRepository = new FakePayoutRepository(accountRepository, ledgerRepository);
        var disputeRepository = new FakeFinancialDisputeRepository(accountRepository, payoutRepository);
        var platformAccount = await accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
        accountRepository.SetProperty(platformAccount.Id, nameof(FinancialAccount.AvailableBalance), 1000m);
        var dispute = FinancialDispute.Open(
            FinancialDisputeCategory.IncorrectFare, Guid.NewGuid(), null, null, 25m, "TND", "Montant incorrect", null, Guid.NewGuid(),
            DateTime.UtcNow);
        await disputeRepository.TryOpenAsync(dispute, null, DateTime.UtcNow, CancellationToken.None);
        var handler = new CreateIncidentFromFinancialDisputeCommandHandler(disputeRepository, _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromFinancialDisputeCommand(dispute.Id, Guid.NewGuid(), SupportIncidentSeverity.Critical),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var request = Assert.Single(_incidentReporter.Requests);
        Assert.Equal("FinancialDispute", request.SourceType);
        Assert.Equal(dispute.Id, request.SourceId);
    }

    [Fact]
    public async Task FromRoadside_ExistingRequest_ReportsIncidentWithSourceAnchor()
    {
        var roadsideRepository = new FakeRoadsideAssistanceRequestRepository();
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.Driver, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur", 36.8, 10.18, null, null, DateTime.UtcNow);
        await roadsideRepository.TryAddAsync(request, CancellationToken.None);
        var handler = new CreateIncidentFromRoadsideCommandHandler(roadsideRepository, _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromRoadsideCommand(request.Id, Guid.NewGuid(), SupportIncidentSeverity.Major), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reportRequest = Assert.Single(_incidentReporter.Requests);
        Assert.Equal("RoadsideAssistanceRequest", reportRequest.SourceType);
        Assert.Equal(request.Id, reportRequest.SourceId);
    }

    [Fact]
    public async Task FromMaintenance_UnknownRequest_ReturnsNotFound()
    {
        var handler = new CreateIncidentFromMaintenanceCommandHandler(new FakeMaintenanceRequestRepository(), _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromMaintenanceCommand(Guid.NewGuid(), Guid.NewGuid(), SupportIncidentSeverity.Minor),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task FromMaintenance_ExistingRequest_ReportsIncidentWithSourceAnchor()
    {
        var maintenanceRepository = new FakeMaintenanceRequestRepository();
        var request = MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Vidange requise", DateTime.UtcNow);
        await maintenanceRepository.TryAddAsync(request, CancellationToken.None);
        var handler = new CreateIncidentFromMaintenanceCommandHandler(maintenanceRepository, _incidentReporter);

        var result = await handler.Handle(
            new CreateIncidentFromMaintenanceCommand(request.Id, Guid.NewGuid(), SupportIncidentSeverity.Moderate),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reportRequest = Assert.Single(_incidentReporter.Requests);
        Assert.Equal("MaintenanceRequest", reportRequest.SourceType);
        Assert.Equal(request.Id, reportRequest.SourceId);
    }
}
