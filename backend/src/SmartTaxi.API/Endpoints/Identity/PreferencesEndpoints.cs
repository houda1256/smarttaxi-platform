using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.Preferences;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Preferences.Commands.UpdateMyPreferences;
using SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.API.Endpoints.Identity;

public static class PreferencesEndpoints
{
    private const string InvalidLanguageError = "Langue inconnue.";
    private const string InvalidChannelError = "Canal de notification inconnu.";

    public static IEndpointRouteBuilder MapPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me/preferences")
            .WithTags("Preferences").RequireAuthorization(Permissions.PreferencesManageOwn);

        group.MapGet("/", GetMineAsync)
            .WithName("GetMyPreferences")
            .Produces<PreferencesResponse>(StatusCodes.Status200OK);

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateMyPreferences")
            .Produces<PreferencesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Ok<PreferencesResponse>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyPreferencesQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summary = await handler.Handle(new GetMyPreferencesQuery(userId), cancellationToken);

        return TypedResults.Ok(PreferencesResponse.FromSummary(summary));
    }

    private static async Task<Results<Ok<PreferencesResponse>, BadRequest<ProblemDetails>>> UpdateAsync(
        UpdatePreferencesRequest request,
        ClaimsPrincipal currentUser,
        UpdateMyPreferencesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Language>(request.Language, ignoreCase: true, out var language))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidLanguageError });
        }

        var channels = NotificationChannel.None;

        foreach (var channelName in request.NotificationChannels)
        {
            if (!Enum.TryParse<NotificationChannel>(channelName, ignoreCase: true, out var channel))
            {
                return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidChannelError });
            }

            channels |= channel;
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new UpdateMyPreferencesCommand(
            userId, language, channels, request.ShareProfileWithPartners, request.AllowMarketingCommunications,
            request.Timezone, request.DisplayName, request.AvatarUrl);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = result.Error });
        }

        return TypedResults.Ok(PreferencesResponse.FromSummary(result.Value!));
    }
}
