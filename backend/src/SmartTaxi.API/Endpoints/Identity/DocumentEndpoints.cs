using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.Documents;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Commands.CancelDocument;
using SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;
using SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentById;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentContent;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocuments;
using SmartTaxi.Application.Identity.Documents.Queries.GetUserProfessionalEligibility;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.API.Endpoints.Identity;

public static class DocumentEndpoints
{
    private const string InvalidDocumentTypeError = "Type de document inconnu.";

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me/documents").WithTags("Documents").RequireAuthorization();

        group.MapPost("/", UploadDocumentAsync)
            .WithName("UploadDocument")
            .RequireAuthorization(Permissions.DocumentsUploadOwn)
            .Produces<UploadDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetMyDocumentsAsync)
            .WithName("GetMyDocuments")
            .RequireAuthorization(Permissions.DocumentsReadOwn)
            .Produces<IReadOnlyCollection<UserDocumentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/eligibility", GetMyEligibilityAsync)
            .WithName("GetMyProfessionalEligibility")
            .RequireAuthorization(Permissions.DocumentsReadOwn)
            .Produces<IReadOnlyCollection<RoleEligibilityResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{documentId:guid}", GetMyDocumentByIdAsync)
            .WithName("GetMyDocumentById")
            .RequireAuthorization(Permissions.DocumentsReadOwn)
            .Produces<UserDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{documentId:guid}/content", DownloadMyDocumentContentAsync)
            .WithName("DownloadMyDocumentContent")
            .RequireAuthorization(Permissions.DocumentsReadOwn)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{documentId:guid}/replace", ReplaceDocumentAsync)
            .WithName("ReplaceDocument")
            .RequireAuthorization(Permissions.DocumentsUploadOwn)
            .Produces<ReplaceDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // REST semantics for the client — internally this never physically
        // deletes the row, it transitions a Pending document to Cancelled.
        group.MapDelete("/{documentId:guid}", CancelDocumentAsync)
            .WithName("CancelDocument")
            .RequireAuthorization(Permissions.DocumentsUploadOwn)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<UploadDocumentResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> UploadDocumentAsync(
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] DateTime? issueDate,
        [FromForm] DateTime? expirationDate,
        ClaimsPrincipal currentUser,
        UploadDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DocumentType>(documentType, ignoreCase: true, out var parsedType))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidDocumentTypeError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await using var stream = file.OpenReadStream();

        var command = new UploadDocumentCommand(userId, parsedType, stream, file.FileName, file.ContentType, issueDate, expirationDate);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.Conflict ? TypedResults.Conflict(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(new UploadDocumentResponse(result.Value!.DocumentId, result.Value.Status.ToString()));
    }

    private static async Task<Ok<IReadOnlyCollection<UserDocumentResponse>>> GetMyDocumentsAsync(
        ClaimsPrincipal currentUser, GetMyDocumentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetMyDocumentsQuery(userId), cancellationToken);

        IReadOnlyCollection<UserDocumentResponse> response = summaries.Select(UserDocumentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<RoleEligibilityResponse>>> GetMyEligibilityAsync(
        ClaimsPrincipal currentUser, GetUserProfessionalEligibilityQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetUserProfessionalEligibilityQuery(userId), cancellationToken);

        IReadOnlyCollection<RoleEligibilityResponse> response = result.Value!
            .Select(r => new RoleEligibilityResponse(r.Role.ToString(), r.IsEligible, r.MissingOrInvalidTypes.Select(t => t.ToString()).ToList()))
            .ToList();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<UserDocumentResponse>, NotFound<ProblemDetails>>> GetMyDocumentByIdAsync(
        Guid documentId, ClaimsPrincipal currentUser, GetMyDocumentByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetMyDocumentByIdQuery(userId, documentId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(UserDocumentResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ProblemDetails>>> DownloadMyDocumentContentAsync(
        Guid documentId, ClaimsPrincipal currentUser, HttpContext httpContext,
        GetMyDocumentContentQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await handler.Handle(new GetMyDocumentContentQuery(userId, documentId, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Results<Ok<ReplaceDocumentResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ReplaceDocumentAsync(
        Guid documentId,
        IFormFile file,
        [FromForm] DateTime? issueDate,
        [FromForm] DateTime? expirationDate,
        ClaimsPrincipal currentUser,
        ReplaceDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await using var stream = file.OpenReadStream();

        var command = new ReplaceDocumentCommand(userId, documentId, stream, file.FileName, file.ContentType, issueDate, expirationDate);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.Ok(new ReplaceDocumentResponse(result.Value!.NewDocumentId, result.Value.NewVersion));
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CancelDocumentAsync(
        Guid documentId, ClaimsPrincipal currentUser, CancelDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new CancelDocumentCommand(userId, documentId), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.Conflict(problem);
        }

        return TypedResults.NoContent();
    }
}
