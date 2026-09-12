using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Preferences;

namespace SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;

public sealed record GetMyPreferencesQuery(Guid UserId) : IQuery<UserPreferencesSummary>;
