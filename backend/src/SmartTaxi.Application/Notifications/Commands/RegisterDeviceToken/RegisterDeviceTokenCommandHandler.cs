using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Commands.RegisterDeviceToken;

/// <summary>
/// A raw token value is only ever registrable for the authenticated caller
/// (UserId comes from the JWT at the API layer, never from the request body) —
/// see the security tests for "cannot register another user's device token".
/// Re-registering a token already owned by someone else reassigns it (phones
/// get reset/resold) rather than erroring, matching DeviceToken.Reassign.
/// </summary>
public sealed class RegisterDeviceTokenCommandHandler : ICommandHandler<RegisterDeviceTokenCommand, Result<Guid>>
{
    private const string RegistrationConflictError = "Impossible d'enregistrer ce jeton d'appareil.";

    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public RegisterDeviceTokenCommandHandler(IDeviceTokenRepository deviceTokenRepository)
    {
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterDeviceTokenCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var existing = await _deviceTokenRepository.GetByTokenAsync(command.Token, cancellationToken);

        if (existing is not null)
        {
            if (existing.UserId != command.UserId)
            {
                existing.Reassign(command.UserId, utcNow);
            }
            else
            {
                existing.Touch(utcNow);
            }

            await _deviceTokenRepository.UpdateAsync(existing, cancellationToken);
            return Result<Guid>.Success(existing.Id);
        }

        DeviceToken token;

        try
        {
            token = DeviceToken.Register(command.UserId, command.Token, command.Platform, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _deviceTokenRepository.TryAddAsync(token, cancellationToken);

        return added ? Result<Guid>.Success(token.Id) : Result<Guid>.Failure(RegistrationConflictError, ErrorType.Conflict);
    }
}
