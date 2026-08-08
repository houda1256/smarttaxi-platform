using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.VehicleDocuments;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ApproveVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ExpireVehicleDocuments;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.RejectVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.SuspendVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetPendingVehicleDocuments;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContentAdmin;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class AdminVehicleDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAdminVehicleDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/fleet/vehicle-documents").WithTags("AdminVehicleDocuments").RequireAuthorization();

        group.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingVehicleDocuments")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReview)
            .Produces<IReadOnlyCollection<VehicleDocumentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{documentId:guid}/content", DownloadContentAdminAsync)
            .WithName("DownloadVehicleDocumentContentAdmin")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReadAll)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{documentId:guid}/approve", ApproveAsync)
            .WithName("ApproveVehicleDocument")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReview)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{documentId:guid}/reject", RejectAsync)
            .WithName("RejectVehicleDocument")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReview)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{documentId:guid}/suspend", SuspendAsync)
            .WithName("SuspendVehicleDocument")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReview)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/expire-sweep", ExpireDueAsync)
            .WithName("ExpireDueVehicleDocuments")
            .RequireAuthorization(Permissions.FleetVehicleDocumentsReview)
            .Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<VehicleDocumentResponse>>> GetPendingAsync(
        GetPendingVehicleDocumentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetPendingVehicleDocumentsQuery(), cancellationToken);
        IReadOnlyCollection<VehicleDocumentResponse> response = summaries.Select(VehicleDocumentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ProblemDetails>>> DownloadContentAdminAsync(
        Guid documentId, ClaimsPrincipal currentUser, HttpContext httpContext,
        GetVehicleDocumentContentAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var requestedBy = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await handler.Handle(new GetVehicleDocumentContentAdminQuery(requestedBy, documentId, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Results<NoContent, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid documentId, ApproveVehicleDocumentRequest request, ClaimsPrincipal currentUser,
        ApproveVehicleDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveVehicleDocumentCommand(reviewerId, documentId, request.ReviewComment), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectAsync(
        Guid documentId, RejectVehicleDocumentRequest request, ClaimsPrincipal currentUser,
        RejectVehicleDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new RejectVehicleDocumentCommand(reviewerId, documentId, request.RejectionReason, request.ReviewComment);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Forbid(),
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid documentId, SuspendVehicleDocumentRequest request, ClaimsPrincipal currentUser,
        SuspendVehicleDocumentCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendVehicleDocumentCommand(reviewerId, documentId, request.ReviewComment), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Ok<int>> ExpireDueAsync(ExpireVehicleDocumentsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ExpireVehicleDocumentsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static Results<NoContent, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>> ToNoContentResult(Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
        return result.ErrorType switch
        {
            ErrorType.Forbidden => TypedResults.Forbid(),
            ErrorType.NotFound => TypedResults.NotFound(problem),
            _ => TypedResults.Conflict(problem)
        };
    }
}
