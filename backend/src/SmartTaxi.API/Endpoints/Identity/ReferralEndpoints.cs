using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity.Referrals;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Referrals.Commands.InvalidateReferral;
using SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferralCode;
using SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferrals;
using SmartTaxi.Application.Identity.Referrals.Queries.GetPendingReferralsAdmin;

namespace SmartTaxi.API.Endpoints.Identity;

public static class ReferralEndpoints
{
    public static IEndpointRouteBuilder MapReferralEndpoints(this IEndpointRouteBuilder app)
    {
        var selfService = app.MapGroup("/api/users/me/referrals").WithTags("Referrals").RequireAuthorization();

        selfService.MapGet("/code", GetMyCodeAsync)
            .WithName("GetMyReferralCode")
            .Produces<ReferralCodeResponse>(StatusCodes.Status200OK);

        selfService.MapGet("/", GetMineAsync)
            .WithName("GetMyReferrals")
            .Produces<IReadOnlyCollection<ReferralResponse>>(StatusCodes.Status200OK);

        var admin = app.MapGroup("/api/admin/referrals").WithTags("AdminReferrals").RequireAuthorization(Permissions.ReferralsManage);

        admin.MapGet("/pending", GetPendingAsync)
            .WithName("GetPendingReferralsAdmin")
            .Produces<IReadOnlyCollection<ReferralResponse>>(StatusCodes.Status200OK);

        admin.MapPost("/{referralId:guid}/invalidate", InvalidateAsync)
            .WithName("InvalidateReferral")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<ReferralCodeResponse>> GetMyCodeAsync(
        ClaimsPrincipal currentUser, GetMyReferralCodeQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetMyReferralCodeQuery(userId), cancellationToken);

        return TypedResults.Ok(new ReferralCodeResponse(result.Value!));
    }

    private static async Task<Ok<IReadOnlyCollection<ReferralResponse>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyReferralsQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetMyReferralsQuery(userId), cancellationToken);

        IReadOnlyCollection<ReferralResponse> response = summaries.Select(ReferralResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<ReferralResponse>>> GetPendingAsync(
        GetPendingReferralsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetPendingReferralsAdminQuery(), cancellationToken);

        IReadOnlyCollection<ReferralResponse> response = summaries.Select(ReferralResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> InvalidateAsync(
        Guid referralId, ClaimsPrincipal currentUser, InvalidateReferralCommandHandler handler, CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new InvalidateReferralCommand(adminId, referralId), cancellationToken);

        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
        return result.ErrorType == Application.Common.ErrorType.NotFound
            ? TypedResults.NotFound(problem)
            : TypedResults.Conflict(problem);
    }
}
