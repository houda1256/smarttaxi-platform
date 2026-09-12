using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetRideMessages;

public sealed class GetRideMessagesQueryHandler : IQueryHandler<GetRideMessagesQuery, Result<IReadOnlyCollection<RideMessageSummary>>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent consulter les messages.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideConversationRepository _conversationRepository;
    private readonly IRideMessageRepository _messageRepository;

    public GetRideMessagesQueryHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository,
        IRideConversationRepository conversationRepository, IRideMessageRepository messageRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result<IReadOnlyCollection<RideMessageSummary>>> Handle(GetRideMessagesQuery query, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(query.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<IReadOnlyCollection<RideMessageSummary>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isParticipant = query.RequestingUserId == ride.CustomerId;

        if (!isParticipant && ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
            isParticipant = driver is not null && driver.UserId == query.RequestingUserId;
        }

        if (!isParticipant)
        {
            return Result<IReadOnlyCollection<RideMessageSummary>>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        var conversation = await _conversationRepository.GetForRideAsync(ride.Id, cancellationToken);

        if (conversation is null)
        {
            return Result<IReadOnlyCollection<RideMessageSummary>>.Success([]);
        }

        var messages = await _messageRepository.GetForConversationAsync(conversation.Id, cancellationToken);
        IReadOnlyCollection<RideMessageSummary> summaries = messages.Select(RideMessageSummary.FromEntity).ToList();

        return Result<IReadOnlyCollection<RideMessageSummary>>.Success(summaries);
    }
}
