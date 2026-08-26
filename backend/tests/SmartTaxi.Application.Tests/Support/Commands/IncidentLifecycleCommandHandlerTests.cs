using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Commands.AcknowledgeSupportIncident;
using SmartTaxi.Application.Support.Commands.CloseSupportIncident;
using SmartTaxi.Application.Support.Commands.InvestigateSupportIncident;
using SmartTaxi.Application.Support.Commands.MarkIncidentFalsePositive;
using SmartTaxi.Application.Support.Commands.ReassignSupportIncident;
using SmartTaxi.Application.Support.Commands.ReopenSupportIncident;
using SmartTaxi.Application.Support.Commands.ResolveSupportIncident;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class IncidentLifecycleCommandHandlerTests
{
    private readonly FakeSupportIncidentRepository _incidentRepository = new();

    private async Task<SupportIncident> CreateIncidentAsync()
    {
        var incident = SupportIncident.Create(
            SupportIncidentType.RideIncident, SupportIncidentSeverity.Moderate, "Titre", "Description", Guid.NewGuid(), null, null, null,
            null, DateTime.UtcNow, null, null, DateTime.UtcNow);
        await _incidentRepository.TryAddAsync(incident, CancellationToken.None);
        return incident;
    }

    private async Task<(SupportIncident Incident, Guid AdminId)> CreateAcknowledgedIncidentAsync()
    {
        var incident = await CreateIncidentAsync();
        var adminId = Guid.NewGuid();
        await _incidentRepository.TryAcknowledgeAsync(incident.Id, adminId, DateTime.UtcNow, CancellationToken.None);
        return (incident, adminId);
    }

    private async Task<(SupportIncident Incident, Guid AdminId)> CreateInvestigatingIncidentAsync()
    {
        var (incident, adminId) = await CreateAcknowledgedIncidentAsync();
        await new InvestigateSupportIncidentCommandHandler(_incidentRepository).Handle(
            new InvestigateSupportIncidentCommand(incident.Id, adminId), CancellationToken.None);
        return (incident, adminId);
    }

    [Fact]
    public async Task Acknowledge_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var incident = await CreateIncidentAsync();
        var handler = new AcknowledgeSupportIncidentCommandHandler(_incidentRepository);

        var first = await handler.Handle(new AcknowledgeSupportIncidentCommand(incident.Id, Guid.NewGuid()), CancellationToken.None);
        var second = await handler.Handle(new AcknowledgeSupportIncidentCommand(incident.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task Reassign_ByAnyAdmin_Succeeds()
    {
        var (incident, _) = await CreateAcknowledgedIncidentAsync();
        var newAdminId = Guid.NewGuid();
        var handler = new ReassignSupportIncidentCommandHandler(_incidentRepository);

        var result = await handler.Handle(new ReassignSupportIncidentCommand(incident.Id, newAdminId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _incidentRepository.GetByIdAsync(incident.Id, CancellationToken.None);
        Assert.Equal(newAdminId, reloaded!.AssignedAdminUserId);
    }

    [Fact]
    public async Task Investigate_ByUnassignedAdmin_ReturnsConflict()
    {
        var (incident, _) = await CreateAcknowledgedIncidentAsync();
        var handler = new InvestigateSupportIncidentCommandHandler(_incidentRepository);

        var result = await handler.Handle(new InvestigateSupportIncidentCommand(incident.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Resolve_ByAssignedAdmin_Succeeds()
    {
        var (incident, adminId) = await CreateInvestigatingIncidentAsync();
        var handler = new ResolveSupportIncidentCommandHandler(_incidentRepository);

        var result = await handler.Handle(
            new ResolveSupportIncidentCommand(incident.Id, adminId, "Incident résolu"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _incidentRepository.GetByIdAsync(incident.Id, CancellationToken.None);
        Assert.Equal(SupportIncidentStatus.Resolved, reloaded!.Status);
    }

    [Fact]
    public async Task Resolve_WithoutResolution_ReturnsValidationError()
    {
        var (incident, adminId) = await CreateInvestigatingIncidentAsync();
        var handler = new ResolveSupportIncidentCommandHandler(_incidentRepository);

        var result = await handler.Handle(new ResolveSupportIncidentCommand(incident.Id, adminId, ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Close_ThenReopen_ByAnyAdmin_Succeeds()
    {
        var (incident, adminId) = await CreateInvestigatingIncidentAsync();
        await new ResolveSupportIncidentCommandHandler(_incidentRepository).Handle(
            new ResolveSupportIncidentCommand(incident.Id, adminId, "Résolu"), CancellationToken.None);

        var closeResult = await new CloseSupportIncidentCommandHandler(_incidentRepository).Handle(
            new CloseSupportIncidentCommand(incident.Id), CancellationToken.None);
        Assert.True(closeResult.IsSuccess);

        var reopenResult = await new ReopenSupportIncidentCommandHandler(_incidentRepository).Handle(
            new ReopenSupportIncidentCommand(incident.Id), CancellationToken.None);
        Assert.True(reopenResult.IsSuccess);

        var reloaded = await _incidentRepository.GetByIdAsync(incident.Id, CancellationToken.None);
        Assert.Equal(SupportIncidentStatus.Reopened, reloaded!.Status);
    }

    [Fact]
    public async Task MarkFalsePositive_FromReported_Succeeds()
    {
        var incident = await CreateIncidentAsync();
        var handler = new MarkIncidentFalsePositiveCommandHandler(_incidentRepository);

        var result = await handler.Handle(new MarkIncidentFalsePositiveCommand(incident.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _incidentRepository.GetByIdAsync(incident.Id, CancellationToken.None);
        Assert.Equal(SupportIncidentStatus.FalsePositive, reloaded!.Status);
    }

    [Fact]
    public async Task MarkFalsePositive_FromResolved_ReturnsConflict()
    {
        var (incident, adminId) = await CreateInvestigatingIncidentAsync();
        await new ResolveSupportIncidentCommandHandler(_incidentRepository).Handle(
            new ResolveSupportIncidentCommand(incident.Id, adminId, "Résolu"), CancellationToken.None);

        var handler = new MarkIncidentFalsePositiveCommandHandler(_incidentRepository);
        var result = await handler.Handle(new MarkIncidentFalsePositiveCommand(incident.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
