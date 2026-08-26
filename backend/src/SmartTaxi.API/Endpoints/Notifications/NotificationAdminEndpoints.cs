using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Notifications;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Notifications.Commands.ActivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.CreateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.DeactivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.ProcessDueNotifications;
using SmartTaxi.Application.Notifications.Commands.ProcessRetryableDeliveries;
using SmartTaxi.Application.Notifications.Commands.UpdateNotificationTemplate;
using SmartTaxi.Application.Notifications.Queries.GetNotificationDeliveryFailures;
using SmartTaxi.Application.Notifications.Queries.GetNotificationTemplates;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.API.Endpoints.Notifications;

public static class NotificationAdminEndpoints
{
    private const string InvalidCategoryError = "Catégorie de notification inconnue.";
    private const string InvalidChannelError = "Canal de notification inconnu.";
    private const string InvalidLanguageError = "Langue inconnue.";

    public static IEndpointRouteBuilder MapNotificationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var templates = app.MapGroup("/api/admin/notification-templates").WithTags("Notification Templates")
            .RequireAuthorization(Permissions.NotificationsTemplatesManage);

        templates.MapGet("/", GetTemplatesAsync)
            .WithName("GetNotificationTemplates").Produces<IReadOnlyCollection<NotificationTemplateResponse>>(StatusCodes.Status200OK);

        templates.MapPost("/", CreateTemplateAsync)
            .WithName("CreateNotificationTemplate").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        templates.MapPut("/{templateId:guid}", UpdateTemplateAsync)
            .WithName("UpdateNotificationTemplate").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        templates.MapPost("/{templateId:guid}/activate", ActivateTemplateAsync)
            .WithName("ActivateNotificationTemplate").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        templates.MapPost("/{templateId:guid}/deactivate", DeactivateTemplateAsync)
            .WithName("DeactivateNotificationTemplate").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        var deliveries = app.MapGroup("/api/admin/notification-deliveries").WithTags("Notification Delivery Monitoring");

        deliveries.MapGet("/failures", GetFailuresAsync).RequireAuthorization(Permissions.NotificationsDeliveryRead)
            .WithName("GetNotificationDeliveryFailures").Produces<PagedResult<NotificationDeliveryFailureResponse>>(StatusCodes.Status200OK);

        deliveries.MapPost("/process-retryable", ProcessRetryableAsync).RequireAuthorization(Permissions.NotificationsDeliveryManage)
            .WithName("ProcessRetryableNotificationDeliveries").Produces<int>(StatusCodes.Status200OK);

        deliveries.MapPost("/process-due", ProcessDueAsync).RequireAuthorization(Permissions.NotificationsDeliveryManage)
            .WithName("ProcessDueNotifications").Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<NotificationTemplateResponse>>> GetTemplatesAsync(
        GetNotificationTemplatesQueryHandler handler, CancellationToken cancellationToken)
    {
        var templates = await handler.Handle(new GetNotificationTemplatesQuery(), cancellationToken);
        IReadOnlyCollection<NotificationTemplateResponse> response = templates.Select(NotificationTemplateResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreateTemplateAsync(
        CreateNotificationTemplateRequest request, CreateNotificationTemplateCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationCategory>(request.Category, ignoreCase: true, out var category))
        {
            return TypedResults.BadRequest(InvalidCategoryError);
        }

        if (!Enum.TryParse<NotificationChannel>(request.Channel, ignoreCase: true, out var channel))
        {
            return TypedResults.BadRequest(InvalidChannelError);
        }

        if (!Enum.TryParse<Language>(request.Language, ignoreCase: true, out var language))
        {
            return TypedResults.BadRequest(InvalidLanguageError);
        }

        var result = await handler.Handle(
            new CreateNotificationTemplateCommand(request.TemplateKey, category, channel, language, request.Subject, request.Body),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateTemplateAsync(
        Guid templateId, UpdateNotificationTemplateRequest request, UpdateNotificationTemplateCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new UpdateNotificationTemplateCommand(templateId, request.Subject, request.Body), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ActivateTemplateAsync(
        Guid templateId, ActivateNotificationTemplateCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ActivateNotificationTemplateCommand(templateId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateTemplateAsync(
        Guid templateId, DeactivateNotificationTemplateCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeactivateNotificationTemplateCommand(templateId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<NotificationDeliveryFailureResponse>>> GetFailuresAsync(
        GetNotificationDeliveryFailuresQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 20)
    {
        var result = await handler.Handle(new GetNotificationDeliveryFailuresQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<NotificationDeliveryFailureResponse>(
            result.Items.Select(NotificationDeliveryFailureResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<int>> ProcessRetryableAsync(
        ProcessRetryableDeliveriesCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessRetryableDeliveriesCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<int>> ProcessDueAsync(ProcessDueNotificationsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessDueNotificationsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
