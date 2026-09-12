using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Support.Commands.ReportSupportIncident;

public sealed class ReportSupportIncidentCommandHandler : ICommandHandler<ReportSupportIncidentCommand, Result<Guid>>
{
    private readonly ISupportIncidentReporter _incidentReporter;

    public ReportSupportIncidentCommandHandler(ISupportIncidentReporter incidentReporter)
    {
        _incidentReporter = incidentReporter;
    }

    public async Task<Result<Guid>> Handle(ReportSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var incidentId = await _incidentReporter.ReportAsync(
                new SupportIncidentReportRequest(
                    command.Type, command.Severity, command.Title, command.Description, command.ReportedByUserId,
                    command.RelatedEntityType, command.RelatedEntityId, command.Latitude, command.Longitude, DateTime.UtcNow,
                    SourceType: null, SourceId: null),
                cancellationToken);

            return Result<Guid>.Success(incidentId);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
