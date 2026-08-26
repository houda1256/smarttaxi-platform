using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Notifications;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Preferences.Queries.GetMyPreferences;
using SmartTaxi.Application.Notifications.Commands.MarkAllNotificationsRead;
using SmartTaxi.Application.Notifications.Commands.MarkNotificationRead;
using SmartTaxi.Application.Notifications.Commands.RegisterDeviceToken;
using SmartTaxi.Application.Notifications.Commands.RevokeDeviceToken;
using SmartTaxi.Application.Notifications.Queries.GetMyNotifications;
using SmartTaxi.Application.Notifications.Queries.GetMyUnreadNotificationCount;

namespace SmartTaxi.API.Endpoints.Notifications;

/// <summary>
/// Self-service surface only — every read/write here is scoped to the caller's
/// own JWT subject claim, never an arbitrary userId from the request (see the
/// security tests for "cannot read/mark-read another user's notification" and
/// "cannot register/revoke another user's device token").
/// </summary>
public static class NotificationEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapGet("/", GetMineAsync).RequireAuthorization(Permissions.NotificationsReadOwn)
            .WithName("GetMyNotifications").Produces<PagedResult<NotificationResponse>>(StatusCodes.Status200OK);

        group.MapGet("/unread-count", GetUnreadCountAsync).RequireAuthorization(Permissions.NotificationsReadOwn)
            .WithName("GetMyUnreadNotificationCount").Produces<int>(StatusCodes.Status200OK);

        group.MapPost("/{notificationId:guid}/read", MarkReadAsync).RequireAuthorization(Permissions.NotificationsReadOwn)
            .WithName("MarkNotificationRead").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/read-all", MarkAllReadAsync).RequireAuthorization(Permissions.NotificationsReadOwn)
            .WithName("MarkAllNotificationsRead").Produces<int>(StatusCodes.Status200OK);

        group.MapGet("/preferences", GetPreferencesAsync).RequireAuthorization(Permissions.NotificationsPreferencesManageOwn)
            .WithName("GetMyNotificationPreferences").Produces<NotificationPreferencesFacadeResponse>(StatusCodes.Status200OK);

        group.MapPost("/device-tokens", RegisterDeviceTokenAsync).RequireAuthorization(Permissions.NotificationsDeviceTokensManageOwn)
            .WithName("RegisterDeviceToken").Produces<DeviceTokenResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/device-tokens/{deviceTokenId:guid}", RevokeDeviceTokenAsync)
            .RequireAuthorization(Permissions.NotificationsDeviceTokensManageOwn)
            .WithName("RevokeDeviceToken").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<PagedResult<NotificationResponse>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyNotificationsQueryHandler handler, CancellationToken cancellationToken,
        bool unreadOnly = false, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(
            new GetMyNotificationsQuery(CurrentUserId(currentUser), unreadOnly, pageNumber, pageSize), cancellationToken);

        var response = new PagedResult<NotificationResponse>(
            result.Items.Select(NotificationResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);

        return TypedResults.Ok(response);
    }

    private static async Task<Ok<int>> GetUnreadCountAsync(
        ClaimsPrincipal currentUser, GetMyUnreadNotificationCountQueryHandler handler, CancellationToken cancellationToken)
    {
        var count = await handler.Handle(new GetMyUnreadNotificationCountQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(count);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkReadAsync(
        Guid notificationId, ClaimsPrincipal currentUser, MarkNotificationReadCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkNotificationReadCommand(notificationId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<int>> MarkAllReadAsync(
        ClaimsPrincipal currentUser, MarkAllNotificationsReadCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkAllNotificationsReadCommand(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<NotificationPreferencesFacadeResponse>> GetPreferencesAsync(
        ClaimsPrincipal currentUser, GetMyPreferencesQueryHandler handler, CancellationToken cancellationToken)
    {
        var summary = await handler.Handle(new GetMyPreferencesQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(NotificationPreferencesFacadeResponse.FromSummary(summary));
    }

    private static async Task<Results<Ok<DeviceTokenResponse>, ProblemHttpResult>> RegisterDeviceTokenAsync(
        RegisterDeviceTokenRequest request, ClaimsPrincipal currentUser, RegisterDeviceTokenCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterDeviceTokenCommand(CurrentUserId(currentUser), request.Token, request.Platform), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(new DeviceTokenResponse(result.Value)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeDeviceTokenAsync(
        Guid deviceTokenId, ClaimsPrincipal currentUser, RevokeDeviceTokenCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RevokeDeviceTokenCommand(deviceTokenId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
