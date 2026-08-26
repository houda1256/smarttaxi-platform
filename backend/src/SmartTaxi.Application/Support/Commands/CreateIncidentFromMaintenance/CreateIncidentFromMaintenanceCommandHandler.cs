using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromMaintenance;

public sealed class CreateIncidentFromMaintenanceCommandHandler : ICommandHandler<CreateIncidentFromMaintenanceCommand, Result<Guid>>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly ISupportIncidentReporter _incidentReporter;

    public CreateIncidentFromMaintenanceCommandHandler(
        IMaintenanceRequestRepository requestRepository, ISupportIncidentReporter incidentReporter)
    {
        _requestRepository = requestRepository;
        _incidentReporter = incidentReporter;
    }

    public async Task<Result<Guid>> Handle(CreateIncidentFromMaintenanceCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var incidentId = await _incidentReporter.ReportAsync(
            new SupportIncidentReportRequest(
                SupportIncidentType.VehicleIncident, command.Severity, $"Incident sur une demande de maintenance {request.Id}",
                request.Description, command.AdminUserId, SupportRelatedEntityType.MaintenanceRequest, request.Id, Latitude: null,
                Longitude: null, DateTime.UtcNow, SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result<Guid>.Success(incidentId);
    }
}
