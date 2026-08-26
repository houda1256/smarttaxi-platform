using SmartTaxi.Application.Support.Commands.ReportSupportIncident;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support.Commands;

public class ReportSupportIncidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_Succeeds()
    {
        var incidentReporter = new FakeSupportIncidentReporter();
        var handler = new ReportSupportIncidentCommandHandler(incidentReporter);

        var result = await handler.Handle(
            new ReportSupportIncidentCommand(
                Guid.NewGuid(), SupportIncidentType.SecurityIncident, SupportIncidentSeverity.Critical, "Titre", "Description", null, null,
                null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var request = Assert.Single(incidentReporter.Requests);
        Assert.Null(request.SourceType);
        Assert.Null(request.SourceId);
    }
}
