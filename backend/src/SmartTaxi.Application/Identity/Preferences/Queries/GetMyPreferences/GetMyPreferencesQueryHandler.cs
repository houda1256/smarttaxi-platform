using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Preferences.Abstractions;

namespace SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;

public sealed class GetMyPreferencesQueryHandler : IQueryHandler<GetMyPreferencesQuery, UserPreferencesSummary>
{
    private readonly IUserPreferencesRepository _repository;

    public GetMyPreferencesQueryHandler(IUserPreferencesRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferencesSummary> Handle(GetMyPreferencesQuery query, CancellationToken cancellationToken)
    {
        var preferences = await _repository.GetByUserIdAsync(query.UserId, cancellationToken);

        return preferences is null ? UserPreferencesSummary.Defaults(query.UserId) : UserPreferencesSummary.FromEntity(preferences);
    }
}
