using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Loyalty;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Loyalty.Commands.RedeemReward;
using SmartTaxi.Application.Loyalty.Queries.GetActiveChallenges;
using SmartTaxi.Application.Loyalty.Queries.GetMyChallengeProgress;
using SmartTaxi.Application.Loyalty.Queries.GetMyLoyaltySummary;
using SmartTaxi.Application.Loyalty.Queries.GetMyPointLedger;
using SmartTaxi.Application.Loyalty.Queries.GetMyReferralRewardStatus;
using SmartTaxi.Application.Loyalty.Queries.GetRewardCatalog;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Endpoints.Loyalty;

/// <summary>
/// Self-service only — every read/write here is scoped to the caller's own
/// JWT subject claim. Referral relationship/status stays on Identity's own
/// /api/users/me/referrals; this only ever surfaces the reward Loyalty granted
/// for a referral, never the relationship itself.
/// </summary>
public static class LoyaltyEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapLoyaltyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loyalty").WithTags("Loyalty");

        group.MapGet("/me", GetMySummaryAsync).RequireAuthorization(Permissions.LoyaltyAccountReadOwn)
            .WithName("GetMyLoyaltySummary").Produces<LoyaltyAccountSummaryResponse?>(StatusCodes.Status200OK);

        group.MapGet("/me/ledger", GetMyLedgerAsync).RequireAuthorization(Permissions.LoyaltyAccountReadOwn)
            .WithName("GetMyPointLedger").Produces<PagedResult<LoyaltyPointLedgerEntryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/rewards", GetRewardCatalogAsync).RequireAuthorization(Permissions.LoyaltyRewardsRead)
            .WithName("GetRewardCatalog").Produces<IReadOnlyCollection<LoyaltyRewardResponse>>(StatusCodes.Status200OK);

        group.MapPost("/rewards/{rewardId:guid}/redeem", RedeemRewardAsync).RequireAuthorization(Permissions.LoyaltyRedemptionCreateOwn)
            .WithName("RedeemReward").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me/referral-rewards", GetMyReferralRewardStatusAsync).RequireAuthorization(Permissions.LoyaltyAccountReadOwn)
            .WithName("GetMyReferralRewardStatus").Produces<IReadOnlyCollection<LoyaltyReferralRewardResponse>>(StatusCodes.Status200OK);

        group.MapGet("/challenges", GetActiveChallengesAsync).RequireAuthorization(Permissions.LoyaltyChallengesRead)
            .WithName("GetActiveChallenges").Produces<IReadOnlyCollection<LoyaltyChallengeResponse>>(StatusCodes.Status200OK);

        group.MapGet("/me/challenges", GetMyChallengeProgressAsync).RequireAuthorization(Permissions.LoyaltyChallengesRead)
            .WithName("GetMyChallengeProgress").Produces<IReadOnlyCollection<LoyaltyChallengeProgressResponse>>(StatusCodes.Status200OK);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    /// <summary>A user may hold several roles — Loyalty only ever has one account per user, so it picks whichever eligible role (Driver over Customer) it recognizes, same rule as LoyaltyReferralRewardGranter.</summary>
    private static UserRole ResolveLoyaltyRole(ClaimsPrincipal currentUser)
    {
        var roles = currentUser.FindAll("role").Select(claim => Enum.Parse<UserRole>(claim.Value)).ToList();
        return roles.Contains(UserRole.Driver) ? UserRole.Driver : UserRole.Customer;
    }

    private static async Task<Ok<LoyaltyAccountSummaryResponse?>> GetMySummaryAsync(
        ClaimsPrincipal currentUser, GetMyLoyaltySummaryQueryHandler handler, CancellationToken cancellationToken)
    {
        var summary = await handler.Handle(new GetMyLoyaltySummaryQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(summary is null ? null : LoyaltyAccountSummaryResponse.FromSummary(summary));
    }

    private static async Task<Ok<PagedResult<LoyaltyPointLedgerEntryResponse>>> GetMyLedgerAsync(
        ClaimsPrincipal currentUser, GetMyPointLedgerQueryHandler handler, CancellationToken cancellationToken,
        int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyPointLedgerQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<LoyaltyPointLedgerEntryResponse>(
            result.Items.Select(LoyaltyPointLedgerEntryResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<LoyaltyRewardResponse>>> GetRewardCatalogAsync(
        ClaimsPrincipal currentUser, GetRewardCatalogQueryHandler handler, CancellationToken cancellationToken)
    {
        var rewards = await handler.Handle(new GetRewardCatalogQuery(ResolveLoyaltyRole(currentUser)), cancellationToken);
        IReadOnlyCollection<LoyaltyRewardResponse> response = rewards.Select(LoyaltyRewardResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RedeemRewardAsync(
        Guid rewardId, RedeemRewardRequest request, ClaimsPrincipal currentUser, RedeemRewardCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RedeemRewardCommand(CurrentUserId(currentUser), rewardId, request.IdempotencyKey), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<LoyaltyReferralRewardResponse>>> GetMyReferralRewardStatusAsync(
        ClaimsPrincipal currentUser, GetMyReferralRewardStatusQueryHandler handler, CancellationToken cancellationToken)
    {
        var rewards = await handler.Handle(new GetMyReferralRewardStatusQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<LoyaltyReferralRewardResponse> response = rewards.Select(LoyaltyReferralRewardResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<LoyaltyChallengeResponse>>> GetActiveChallengesAsync(
        ClaimsPrincipal currentUser, GetActiveChallengesQueryHandler handler, CancellationToken cancellationToken)
    {
        var challenges = await handler.Handle(new GetActiveChallengesQuery(ResolveLoyaltyRole(currentUser)), cancellationToken);
        IReadOnlyCollection<LoyaltyChallengeResponse> response = challenges.Select(LoyaltyChallengeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<LoyaltyChallengeProgressResponse>>> GetMyChallengeProgressAsync(
        ClaimsPrincipal currentUser, GetMyChallengeProgressQueryHandler handler, CancellationToken cancellationToken)
    {
        var progress = await handler.Handle(new GetMyChallengeProgressQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<LoyaltyChallengeProgressResponse> response = progress.Select(LoyaltyChallengeProgressResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }
}
