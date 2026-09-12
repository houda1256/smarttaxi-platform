using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.VehicleDocuments;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.ReplaceVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Commands.UploadVehicleDocument;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocumentContent;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Queries.GetVehicleDocuments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class VehicleDocumentEndpoints
{
    private const string InvalidDocumentTypeError = "Type de document inconnu.";

    public static IEndpointRouteBuilder MapVehicleDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/vehicles/{vehicleId:guid}/documents")
            .WithTags("VehicleDocuments").RequireAuthorization(Permissions.FleetVehicleDocumentsManageOwn);

        group.MapPost("/", UploadAsync)
            .WithName("UploadVehicleDocument")
            .Produces<UploadVehicleDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetForVehicleAsync)
            .WithName("GetVehicleDocuments")
            .Produces<IReadOnlyCollection<VehicleDocumentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{documentId:guid}/content", DownloadContentAsync)
            .WithName("DownloadVehicleDocumentContent")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{documentId:guid}/replace", ReplaceAsync)
            .WithName("ReplaceVehicleDocument")
            .Produces<ReplaceVehicleDocumentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<UploadVehicleDocumentResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> UploadAsync(
        Guid vehicleId,
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] DateTime? issueDate,
        [FromForm] DateTime? expirationDate,
        ClaimsPrincipal currentUser,
        UploadVehicleDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<VehicleDocumentType>(documentType, ignoreCase: true, out var parsedType))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidDocumentTypeError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await using var stream = file.OpenReadStream();

        var command = new UploadVehicleDocumentCommand(userId, vehicleId, parsedType, stream, file.FileName, file.ContentType, issueDate, expirationDate);
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

        return TypedResults.Ok(new UploadVehicleDocumentResponse(result.Value));
    }

    private static async Task<Results<Ok<IReadOnlyCollection<VehicleDocumentResponse>>, NotFound<ProblemDetails>>> GetForVehicleAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, GetVehicleDocumentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetVehicleDocumentsQuery(userId, vehicleId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Véhicule introuvable", Detail = result.Error });
        }

        IReadOnlyCollection<VehicleDocumentResponse> response = result.Value!.Select(VehicleDocumentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ProblemDetails>>> DownloadContentAsync(
        Guid vehicleId, Guid documentId, ClaimsPrincipal currentUser, HttpContext httpContext,
        GetVehicleDocumentContentQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await handler.Handle(new GetVehicleDocumentContentQuery(userId, documentId, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Document introuvable", Detail = result.Error });
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Results<Ok<ReplaceVehicleDocumentResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ReplaceAsync(
        Guid vehicleId,
        Guid documentId,
        IFormFile file,
        [FromForm] DateTime? issueDate,
        [FromForm] DateTime? expirationDate,
        ClaimsPrincipal currentUser,
        ReplaceVehicleDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await using var stream = file.OpenReadStream();

        var command = new ReplaceVehicleDocumentCommand(userId, documentId, stream, file.FileName, file.ContentType, issueDate, expirationDate);
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

        return TypedResults.Ok(new ReplaceVehicleDocumentResponse(result.Value));
    }
}
