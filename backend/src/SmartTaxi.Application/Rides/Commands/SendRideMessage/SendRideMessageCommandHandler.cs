using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SendRideMessage;

/// <summary>
/// Once a Ride reaches a terminal status, the conversation becomes read-only
/// after IRideServicePolicy.ConversationReadOnlyAfterMinutes — checked lazily
/// here (no background job) using the Ride's UpdatedAt as the terminal-
/// transition timestamp.
/// </summary>
public sealed class SendRideMessageCommandHandler : ICommandHandler<SendRideMessageCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent échanger des messages.";
    private const string ReadOnlyError = "Cette conversation est en lecture seule.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideConversationRepository _conversationRepository;
    private readonly IRideMessageRepository _messageRepository;
    private readonly IRideServicePolicy _servicePolicy;

    public SendRideMessageCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository,
        IRideConversationRepository conversationRepository, IRideMessageRepository messageRepository,
        IRideServicePolicy servicePolicy)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _servicePolicy = servicePolicy;
    }

    public async Task<Result<Guid>> Handle(SendRideMessageCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isParticipant = command.RequestingUserId == ride.CustomerId;

        if (!isParticipant && ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
            isParticipant = driver is not null && driver.UserId == command.RequestingUserId;
        }

        if (!isParticipant)
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        var utcNow = DateTime.UtcNow;
        var conversation = await _conversationRepository.GetForRideAsync(ride.Id, cancellationToken);

        if (conversation is null)
        {
            conversation = RideConversation.CreateForRide(ride.Id, utcNow);
            await _conversationRepository.AddAsync(conversation, cancellationToken);
        }

        if (conversation.Status == RideConversationStatus.Active
            && RideStatusTransitions.IsTerminal(ride.Status)
            && (utcNow - ride.UpdatedAt).TotalMinutes >= _servicePolicy.ConversationReadOnlyAfterMinutes)
        {
            await _conversationRepository.TryMarkReadOnlyAsync(conversation.Id, utcNow, cancellationToken);
            return Result<Guid>.Failure(ReadOnlyError, ErrorType.Conflict);
        }

        if (conversation.Status != RideConversationStatus.Active)
        {
            return Result<Guid>.Failure(ReadOnlyError, ErrorType.Conflict);
        }

        RideMessage message;

        try
        {
            message = new RideMessage(conversation.Id, command.RequestingUserId, command.MessageType, command.Content, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _messageRepository.AddAsync(message, cancellationToken);

        return Result<Guid>.Success(message.Id);
    }
}
