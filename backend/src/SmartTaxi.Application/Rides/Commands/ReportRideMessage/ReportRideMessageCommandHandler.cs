using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.ReportRideMessage;

public sealed class ReportRideMessageCommandHandler : ICommandHandler<ReportRideMessageCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent signaler un message.";
    private const string MessageNotFoundError = "Message introuvable.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideMessageRepository _messageRepository;

    public ReportRideMessageCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IRideMessageRepository messageRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result> Handle(ReportRideMessageCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isParticipant = command.RequestingUserId == ride.CustomerId;

        if (!isParticipant && ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
            isParticipant = driver is not null && driver.UserId == command.RequestingUserId;
        }

        if (!isParticipant)
        {
            return Result.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        var reported = await _messageRepository.TryReportAsync(command.MessageId, cancellationToken);

        return reported ? Result.Success() : Result.Failure(MessageNotFoundError, ErrorType.NotFound);
    }
}
