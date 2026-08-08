using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SubmitRideRating;

public sealed class SubmitRideRatingCommandHandler : ICommandHandler<SubmitRideRatingCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotCompletedError = "La course doit être terminée pour être notée.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent la noter.";
    private const string AlreadyRatedError = "Vous avez déjà noté cette course.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideRatingRepository _ratingRepository;

    public SubmitRideRatingCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IRideRatingRepository ratingRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _ratingRepository = ratingRepository;
    }

    public async Task<Result<Guid>> Handle(SubmitRideRatingCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (ride.Status is not (RideStatus.AwaitingPayment or RideStatus.Completed) || ride.SelectedDriverId is null)
        {
            return Result<Guid>.Failure(NotCompletedError, ErrorType.Validation);
        }

        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

        if (driver is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        Guid reviewedUserId;

        if (command.RequestingUserId == ride.CustomerId)
        {
            reviewedUserId = driver.UserId;
        }
        else if (command.RequestingUserId == driver.UserId)
        {
            reviewedUserId = ride.CustomerId;
        }
        else
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        var alreadyRated = await _ratingRepository.ExistsForReviewerAndRideAsync(ride.Id, command.RequestingUserId, cancellationToken);

        if (alreadyRated)
        {
            return Result<Guid>.Failure(AlreadyRatedError, ErrorType.Conflict);
        }

        RideRating rating;

        try
        {
            rating = RideRating.Submit(
                ride.Id, command.RequestingUserId, reviewedUserId, command.Score, command.Comment, command.Tags,
                DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _ratingRepository.TryAddAsync(rating, cancellationToken);

        return added ? Result<Guid>.Success(rating.Id) : Result<Guid>.Failure(AlreadyRatedError, ErrorType.Conflict);
    }
}
