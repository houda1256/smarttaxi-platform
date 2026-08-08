using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.DataRequests;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.DataRequests.Commands.ProcessPersonalDataRequest;
using SmartTaxi.Application.Identity.DataRequests.Commands.SubmitPersonalDataRequest;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataExportContent;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataRequests;
using SmartTaxi.Application.Identity.DataRequests.Queries.GetPendingPersonalDataRequestsAdmin;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.API.Endpoints.Identity;

public static class DataRequestEndpoints
{
    private const string InvalidRequestTypeError = "Type de demande inconnu.";

    public static IEndpointRouteBuilder MapDataRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var selfService = app.MapGroup("/api/users/me/data-requests")
            .WithTags("PersonalDataRequests").RequireAuthorization(Permissions.DataRequestsSubmitOwn);

        selfService.MapPost("/", SubmitAsync)
            .WithName("SubmitPersonalDataRequest")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        selfService.MapGet("/", GetMineAsync)
            .WithName("GetMyPersonalDataRequests")
            .Produces<IReadOnlyCollection<PersonalDataRequestResponse>>(StatusCodes.Status200OK);

        selfService.MapGet("/{requestId:guid}/content", DownloadExportContentAsync)
            .WithName("DownloadMyPersonalDataExportContent")
            .ProducesProblem(StatusCodes.Status404NotFound);

        var admin = app.MapGroup("/api/admin/data-requests")
            .WithTags("AdminPersonalDataRequests").RequireAuthorization(Permissions.DataRequestsProcess);

        admin.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingPersonalDataRequestsAdmin")
            .Produces<IReadOnlyCollection<PersonalDataRequestResponse>>(StatusCodes.Status200OK);

        admin.MapPost("/{requestId:guid}/process", ProcessAsync)
            .WithName("ProcessPersonalDataRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> SubmitAsync(
        SubmitPersonalDataRequestRequest request,
        ClaimsPrincipal currentUser,
        SubmitPersonalDataRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PersonalDataRequestType>(request.RequestType, ignoreCase: true, out var requestType))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidRequestTypeError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitPersonalDataRequestCommand(userId, requestType), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.Conflict ? TypedResults.Conflict(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyCollection<PersonalDataRequestResponse>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyPersonalDataRequestsQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetMyPersonalDataRequestsQuery(userId), cancellationToken);

        IReadOnlyCollection<PersonalDataRequestResponse> response =
            summaries.Select(PersonalDataRequestResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ProblemDetails>>> DownloadExportContentAsync(
        Guid requestId, ClaimsPrincipal currentUser, GetMyPersonalDataExportContentQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetMyPersonalDataExportContentQuery(userId, requestId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Export introuvable", Detail = result.Error });
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Ok<IReadOnlyCollection<PersonalDataRequestResponse>>> GetPendingAsync(
        GetPendingPersonalDataRequestsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetPendingPersonalDataRequestsAdminQuery(), cancellationToken);

        IReadOnlyCollection<PersonalDataRequestResponse> response =
            summaries.Select(PersonalDataRequestResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ProcessAsync(
        Guid requestId, ProcessPersonalDataRequestRequest request, ClaimsPrincipal currentUser,
        ProcessPersonalDataRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var processedBy = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new ProcessPersonalDataRequestCommand(processedBy, requestId, request.Approve, request.ProcessingNotes);
        var result = await handler.Handle(command, cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
        return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.Conflict(problem);
    }
}
