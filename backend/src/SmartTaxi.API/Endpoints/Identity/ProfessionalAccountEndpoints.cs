using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.Professional;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Professional.Commands.ApproveProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.ReactivateProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.RejectProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.SubmitProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Commands.SuspendProfessionalAccountRequest;
using SmartTaxi.Application.Identity.Professional.Queries.GetMyProfessionalAccountRequests;
using SmartTaxi.Application.Identity.Professional.Queries.GetPendingProfessionalAccountRequests;
using SmartTaxi.Application.Identity.Professional.Queries.GetProfessionalAccountRequestByIdAdmin;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.API.Endpoints.Identity;

public static class ProfessionalAccountEndpoints
{
    private const string InvalidRoleError = "Rôle inconnu.";

    public static IEndpointRouteBuilder MapProfessionalAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var selfService = app.MapGroup("/api/users/me/professional-requests")
            .WithTags("ProfessionalAccounts").RequireAuthorization(Permissions.ProfessionalRegisterOwn);

        selfService.MapPost("/", SubmitAsync)
            .WithName("SubmitProfessionalAccountRequest")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        selfService.MapGet("/", GetMineAsync)
            .WithName("GetMyProfessionalAccountRequests")
            .Produces<IReadOnlyCollection<ProfessionalAccountRequestResponse>>(StatusCodes.Status200OK);

        var admin = app.MapGroup("/api/admin/professional-requests")
            .WithTags("AdminProfessionalAccounts").RequireAuthorization(Permissions.ProfessionalReview);

        admin.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingProfessionalAccountRequests")
            .Produces<IReadOnlyCollection<ProfessionalAccountRequestResponse>>(StatusCodes.Status200OK);

        admin.MapGet("/{requestId:guid}", GetByIdAsync)
            .WithName("GetProfessionalAccountRequestByIdAdmin")
            .Produces<ProfessionalAccountRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapPost("/{requestId:guid}/approve", ApproveAsync)
            .WithName("ApproveProfessionalAccountRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/{requestId:guid}/reject", RejectAsync)
            .WithName("RejectProfessionalAccountRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/{requestId:guid}/suspend", SuspendAsync)
            .WithName("SuspendProfessionalAccountRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/{requestId:guid}/reactivate", ReactivateAsync)
            .WithName("ReactivateProfessionalAccountRequest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> SubmitAsync(
        SubmitProfessionalAccountRequestRequest request,
        ClaimsPrincipal currentUser,
        SubmitProfessionalAccountRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidRoleError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitProfessionalAccountRequestCommand(userId, role), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.Conflict ? TypedResults.Conflict(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyCollection<ProfessionalAccountRequestResponse>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyProfessionalAccountRequestsQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetMyProfessionalAccountRequestsQuery(userId), cancellationToken);

        IReadOnlyCollection<ProfessionalAccountRequestResponse> response =
            summaries.Select(ProfessionalAccountRequestResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<ProfessionalAccountRequestResponse>>> GetPendingAsync(
        GetPendingProfessionalAccountRequestsQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetPendingProfessionalAccountRequestsQuery(), cancellationToken);

        IReadOnlyCollection<ProfessionalAccountRequestResponse> response =
            summaries.Select(ProfessionalAccountRequestResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ProfessionalAccountRequestResponse>, NotFound<ProblemDetails>>> GetByIdAsync(
        Guid requestId, GetProfessionalAccountRequestByIdAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetProfessionalAccountRequestByIdAdminQuery(requestId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Demande introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(ProfessionalAccountRequestResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid requestId, ReviewProfessionalAccountRequestRequest request, ClaimsPrincipal currentUser,
        ApproveProfessionalAccountRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(
            new ApproveProfessionalAccountRequestCommand(reviewerId, requestId, request.ReviewComment), cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectAsync(
        Guid requestId, RejectProfessionalAccountRequestRequest request, ClaimsPrincipal currentUser,
        RejectProfessionalAccountRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new RejectProfessionalAccountRequestCommand(reviewerId, requestId, request.RejectionReason, request.ReviewComment);
        var result = await handler.Handle(command, cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid requestId, ReviewProfessionalAccountRequestRequest request, ClaimsPrincipal currentUser,
        SuspendProfessionalAccountRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(
            new SuspendProfessionalAccountRequestCommand(reviewerId, requestId, request.ReviewComment), cancellationToken);

        return ToResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ReactivateAsync(
        Guid requestId, ReviewProfessionalAccountRequestRequest request, ClaimsPrincipal currentUser,
        ReactivateProfessionalAccountRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(
            new ReactivateProfessionalAccountRequestCommand(reviewerId, requestId, request.ReviewComment), cancellationToken);

        return ToResult(result);
    }

    private static Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>> ToResult(Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };

        return result.ErrorType switch
        {
            ErrorType.NotFound => TypedResults.NotFound(problem),
            ErrorType.Conflict => TypedResults.Conflict(problem),
            _ => TypedResults.BadRequest(problem)
        };
    }
}
