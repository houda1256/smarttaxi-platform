using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Commands.RevokeRideShareToken;

public sealed class RevokeRideShareTokenCommandHandler : ICommandHandler<RevokeRideShareTokenCommand, Result>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NoActiveTokenError = "Aucun lien de partage actif pour cette course.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideShareTokenRepository _tokenRepository;

    public RevokeRideShareTokenCommandHandler(IRideRepository rideRepository, IRideShareTokenRepository tokenRepository)
    {
        _rideRepository = rideRepository;
        _tokenRepository = tokenRepository;
    }

    public async Task<Result> Handle(RevokeRideShareTokenCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var token = await _tokenRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (token is null)
        {
            return Result.Failure(NoActiveTokenError, ErrorType.NotFound);
        }

        var revoked = await _tokenRepository.TryRevokeAsync(token.Id, DateTime.UtcNow, cancellationToken);

        return revoked ? Result.Success() : Result.Failure(NoActiveTokenError, ErrorType.Conflict);
    }
}
