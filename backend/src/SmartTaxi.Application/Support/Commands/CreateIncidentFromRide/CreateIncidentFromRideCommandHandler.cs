using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CreateIncidentFromRide;

public sealed class CreateIncidentFromRideCommandHandler : ICommandHandler<CreateIncidentFromRideCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";

    private readonly IRideRepository _rideRepository;
    private readonly ISupportIncidentReporter _incidentReporter;

    public CreateIncidentFromRideCommandHandler(IRideRepository rideRepository, ISupportIncidentReporter incidentReporter)
    {
        _rideRepository = rideRepository;
        _incidentReporter = incidentReporter;
    }

    public async Task<Result<Guid>> Handle(CreateIncidentFromRideCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var description = string.IsNullOrWhiteSpace(command.DescriptionOverride)
            ? $"Incident signalé par un administrateur sur la course {ride.RideNumber}."
            : command.DescriptionOverride;

        var incidentId = await _incidentReporter.ReportAsync(
            new SupportIncidentReportRequest(
                command.Type, command.Severity, $"Incident sur la course {ride.RideNumber}", description, command.AdminUserId,
                SupportRelatedEntityType.Ride, ride.Id, Latitude: null, Longitude: null, DateTime.UtcNow, SourceType: "Ride",
                SourceId: ride.Id),
            cancellationToken);

        return Result<Guid>.Success(incidentId);
    }
}
