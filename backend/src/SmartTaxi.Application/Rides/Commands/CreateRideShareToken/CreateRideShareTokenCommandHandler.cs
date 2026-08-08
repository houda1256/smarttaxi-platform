using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Commands.CreateRideShareToken;

/// <summary>Only the Customer may create a share link for their own Ride. Creating a new one revokes any previously active link — one live link per Ride at a time.</summary>
public sealed class CreateRideShareTokenCommandHandler : ICommandHandler<CreateRideShareTokenCommand, Result<string>>
{
    private const string NotFoundError = "Course introuvable.";

    private readonly IRideRepository _rideRepository;
    private readonly IRideShareTokenRepository _tokenRepository;
    private readonly IRideShareTokenGenerator _tokenGenerator;
    private readonly IRideShareTokenHasher _tokenHasher;
    private readonly IRideServicePolicy _servicePolicy;

    public CreateRideShareTokenCommandHandler(
        IRideRepository rideRepository, IRideShareTokenRepository tokenRepository,
        IRideShareTokenGenerator tokenGenerator, IRideShareTokenHasher tokenHasher, IRideServicePolicy servicePolicy)
    {
        _rideRepository = rideRepository;
        _tokenRepository = tokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _servicePolicy = servicePolicy;
    }

    public async Task<Result<string>> Handle(CreateRideShareTokenCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.CustomerId != command.RequestingUserId)
        {
            return Result<string>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var existing = await _tokenRepository.GetActiveForRideAsync(ride.Id, cancellationToken);

        if (existing is not null)
        {
            await _tokenRepository.TryRevokeAsync(existing.Id, utcNow, cancellationToken);
        }

        var rawToken = _tokenGenerator.Generate();
        var tokenHash = _tokenHasher.Hash(rawToken);
        var validity = TimeSpan.FromHours(_servicePolicy.RideShareTokenValidityHours);
        var token = RideShareToken.Create(ride.Id, tokenHash, utcNow, validity);

        await _tokenRepository.AddAsync(token, cancellationToken);

        return Result<string>.Success(rawToken);
    }
}
