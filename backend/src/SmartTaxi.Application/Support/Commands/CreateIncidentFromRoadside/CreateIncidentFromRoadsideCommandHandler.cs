using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromRoadside;

public sealed class CreateIncidentFromRoadsideCommandHandler : ICommandHandler<CreateIncidentFromRoadsideCommand, Result<Guid>>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly ISupportIncidentReporter _incidentReporter;

    public CreateIncidentFromRoadsideCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, ISupportIncidentReporter incidentReporter)
    {
        _requestRepository = requestRepository;
        _incidentReporter = incidentReporter;
    }

    public async Task<Result<Guid>> Handle(CreateIncidentFromRoadsideCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var incidentId = await _incidentReporter.ReportAsync(
            new SupportIncidentReportRequest(
                SupportIncidentType.VehicleIncident, command.Severity, $"Incident sur une assistance routière {request.Id}",
                request.Description, command.AdminUserId, SupportRelatedEntityType.RoadsideAssistanceRequest, request.Id,
                request.Latitude, request.Longitude, DateTime.UtcNow, SourceType: "RoadsideAssistanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result<Guid>.Success(incidentId);
    }
}
