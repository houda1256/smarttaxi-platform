using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.RevokeDeviceToken;

public sealed class RevokeDeviceTokenCommandHandler : ICommandHandler<RevokeDeviceTokenCommand, Result>
{
    private const string NotFoundError = "Jeton d'appareil introuvable.";
    private const string NotOwnerError = "Seul le propriétaire peut révoquer ce jeton d'appareil.";

    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public RevokeDeviceTokenCommandHandler(IDeviceTokenRepository deviceTokenRepository)
    {
        _deviceTokenRepository = deviceTokenRepository;
    }

    public async Task<Result> Handle(RevokeDeviceTokenCommand command, CancellationToken cancellationToken)
    {
        var token = await _deviceTokenRepository.GetByIdAsync(command.DeviceTokenId, cancellationToken);

        if (token is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (token.UserId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        token.Revoke(DateTime.UtcNow);
        await _deviceTokenRepository.UpdateAsync(token, cancellationToken);

        return Result.Success();
    }
}
