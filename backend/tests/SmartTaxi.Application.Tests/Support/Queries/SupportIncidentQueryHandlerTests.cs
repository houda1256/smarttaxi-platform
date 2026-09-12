using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Queries.GetAllSupportIncidents;
using SmartTaxi.Application.Support.Queries.GetSupportIncidentById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Queries;

public class SupportIncidentQueryHandlerTests
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

    [Fact]
    public async Task GetAll_ReturnsEveryIncident()
    {
        await CreateIncidentAsync();
        await CreateIncidentAsync();

        var handler = new GetAllSupportIncidentsQueryHandler(_incidentRepository);
        var result = await handler.Handle(new GetAllSupportIncidentsQuery(1, 10), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetById_Existing_Succeeds()
    {
        var incident = await CreateIncidentAsync();
        var handler = new GetSupportIncidentByIdQueryHandler(_incidentRepository);

        var result = await handler.Handle(new GetSupportIncidentByIdQuery(incident.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(incident.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFound()
    {
        var handler = new GetSupportIncidentByIdQueryHandler(_incidentRepository);

        var result = await handler.Handle(new GetSupportIncidentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
