using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.Documents;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Commands.ApproveDocument;
using SmartTaxi.Application.Identity.Documents.Commands.ExpireDocuments;
using SmartTaxi.Application.Identity.Documents.Commands.RejectDocument;
using SmartTaxi.Application.Identity.Documents.Commands.SuspendDocument;
using SmartTaxi.Application.Identity.Documents.Queries.GetDocumentByIdAdmin;
using SmartTaxi.Application.Identity.Documents.Queries.GetDocumentContentAdmin;
using SmartTaxi.Application.Identity.Documents.Queries.GetPendingDocuments;

namespace SmartTaxi.API.Endpoints.Identity;

public static class AdminDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAdminDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/documents").WithTags("AdminDocuments").RequireAuthorization();

        group.MapGet("/pending", GetPendingDocumentsAsync)
            .WithName("GetPendingDocuments")
            .RequireAuthorization(Permissions.DocumentsReview)
            .Produces<IReadOnlyCollection<UserDocumentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{documentId:guid}", GetDocumentByIdAsync)
            .WithName("GetDocumentByIdAdmin")
            .RequireAuthorization(Permissions.DocumentsReadAll)
            .Produces<UserDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{documentId:guid}/content", DownloadDocumentContentAsync)
            .WithName("DownloadDocumentContentAdmin")
            .RequireAuthorization(Permissions.DocumentsReadAll)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{documentId:guid}/approve", ApproveDocumentAsync)
            .WithName("ApproveDocument")
            .RequireAuthorization(Permissions.DocumentsReview)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{documentId:guid}/reject", RejectDocumentAsync)
            .WithName("RejectDocument")
            .RequireAuthorization(Permissions.DocumentsReview)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{documentId:guid}/suspend", SuspendDocumentAsync)
            .WithName("SuspendDocument")
            .RequireAuthorization(Permissions.DocumentsSuspend)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/expire-sweep", ExpireDueDocumentsAsync)
            .WithName("ExpireDueDocuments")
            .RequireAuthorization(Permissions.DocumentsConfigure)
            .Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<UserDocumentResponse>>> GetPendingDocumentsAsync(
        GetPendingDocumentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetPendingDocumentsQuery(), cancellationToken);
        IReadOnlyCollection<UserDocumentResponse> response = summaries.Select(UserDocumentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<UserDocumentResponse>, NotFound<ProblemDetails>>> GetDocumentByIdAsync(
        Guid documentId, ClaimsPrincipal currentUser, HttpContext httpContext,
        GetDocumentByIdAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var requestedBy = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await handler.Handle(new GetDocumentByIdAdminQuery(requestedBy, documentId, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(UserDocumentResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ProblemDetails>>> DownloadDocumentContentAsync(
        Guid documentId, ClaimsPrincipal currentUser, HttpContext httpContext,
        GetDocumentContentAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var requestedBy = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await handler.Handle(new GetDocumentContentAdminQuery(requestedBy, documentId, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Results<NoContent, ProblemHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveDocumentAsync(
        Guid documentId, ApproveDocumentRequest request, ClaimsPrincipal currentUser,
        ApproveDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveDocumentCommand(reviewerId, documentId, request.ReviewComment), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ProblemHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectDocumentAsync(
        Guid documentId, RejectDocumentRequest request, ClaimsPrincipal currentUser,
        RejectDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new RejectDocumentCommand(reviewerId, documentId, request.RejectionReason, request.ReviewComment);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Problem(title: "Interdit", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendDocumentAsync(
        Guid documentId, SuspendDocumentRequest request, ClaimsPrincipal currentUser,
        SuspendDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendDocumentCommand(reviewerId, documentId, request.ReviewComment), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Ok<int>> ExpireDueDocumentsAsync(
        ExpireDocumentsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ExpireDocumentsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value!.ExpiredCount);
    }

    private static Results<NoContent, ProblemHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>> ToFailureResult(Result result)
    {
        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };

        return result.ErrorType switch
        {
            ErrorType.Forbidden => TypedResults.Problem(title: "Interdit", detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.NotFound => TypedResults.NotFound(problem),
            _ => TypedResults.Conflict(problem)
        };
    }
}
