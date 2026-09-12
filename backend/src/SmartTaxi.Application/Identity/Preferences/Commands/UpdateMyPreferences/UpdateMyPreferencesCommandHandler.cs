using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Preferences.Abstractions;
using SmartTaxi.Domain.Identity.Preferences.Entities;

namespace SmartTaxi.Application.Identity.Preferences.Commands.UpdateMyPreferences;

public sealed class UpdateMyPreferencesCommandHandler
    : ICommandHandler<UpdateMyPreferencesCommand, Result<UserPreferencesSummary>>
{
    private const string InvalidTimezoneError = "Le fuseau horaire indiqué est invalide.";

    private readonly IUserPreferencesRepository _repository;

    public UpdateMyPreferencesCommandHandler(IUserPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<UserPreferencesSummary>> Handle(
        UpdateMyPreferencesCommand command, CancellationToken cancellationToken)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(command.Timezone, out _))
        {
            return Result<UserPreferencesSummary>.Failure(InvalidTimezoneError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var preferences = await _repository.GetByUserIdAsync(command.UserId, cancellationToken);
        var isNew = preferences is null;
        preferences ??= UserPreferences.CreateDefault(command.UserId, utcNow);

        preferences.Update(
            command.Language,
            command.NotificationChannels,
            command.ShareProfileWithPartners,
            command.AllowMarketingCommunications,
            command.Timezone,
            command.DisplayName,
            command.AvatarUrl,
            utcNow);

        if (isNew)
        {
            await _repository.AddAsync(preferences, cancellationToken);
        }
        else
        {
            await _repository.UpdateAsync(preferences, cancellationToken);
        }

        return Result<UserPreferencesSummary>.Success(UserPreferencesSummary.FromEntity(preferences));
    }
}
