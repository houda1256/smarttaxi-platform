using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Loyalty;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Loyalty.Commands.ActivateChallenge;
using SmartTaxi.Application.Loyalty.Commands.ActivateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.ActivateReward;
using SmartTaxi.Application.Loyalty.Commands.AdjustPoints;
using SmartTaxi.Application.Loyalty.Commands.CreateChallenge;
using SmartTaxi.Application.Loyalty.Commands.CreateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.CreateReward;
using SmartTaxi.Application.Loyalty.Commands.DeactivateChallenge;
using SmartTaxi.Application.Loyalty.Commands.DeactivateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.DeactivateReward;
using SmartTaxi.Application.Loyalty.Commands.ProcessExpiredPoints;
using SmartTaxi.Application.Loyalty.Commands.ProcessReferralRewards;
using SmartTaxi.Application.Loyalty.Commands.UpdateEarningRule;
using SmartTaxi.Application.Loyalty.Commands.UpdateReward;
using SmartTaxi.Application.Loyalty.Commands.UpdateTierThreshold;
using SmartTaxi.Application.Loyalty.Queries.GetChallengesAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetEarningRulesAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetRewardCatalogAdmin;
using SmartTaxi.Application.Loyalty.Queries.GetTierThresholdsAdmin;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.API.Endpoints.Loyalty;

public static class LoyaltyAdminEndpoints
{
    private const string InvalidRoleError = "Rôle inconnu.";
    private const string InvalidCriteriaError = "Type de critère de défi inconnu.";
    private const string InvalidRewardTypeError = "Type de récompense inconnu.";
    private const string InvalidPointTypeError = "Type de points inconnu.";
    private const string InvalidTierError = "Palier inconnu.";

    public static IEndpointRouteBuilder MapLoyaltyAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var rules = app.MapGroup("/api/admin/loyalty/earning-rules").WithTags("Loyalty Admin - Earning Rules")
            .RequireAuthorization(Permissions.LoyaltyRulesManage);

        rules.MapGet("/", GetEarningRulesAsync).WithName("GetEarningRulesAdmin").Produces<IReadOnlyCollection<LoyaltyEarningRuleResponse>>(StatusCodes.Status200OK);
        rules.MapPost("/", CreateEarningRuleAsync).WithName("CreateEarningRule").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);
        rules.MapPut("/{ruleId:guid}", UpdateEarningRuleAsync).WithName("UpdateEarningRule").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);
        rules.MapPost("/{ruleId:guid}/activate", ActivateEarningRuleAsync).WithName("ActivateEarningRule").Produces(StatusCodes.Status204NoContent);
        rules.MapPost("/{ruleId:guid}/deactivate", DeactivateEarningRuleAsync).WithName("DeactivateEarningRule").Produces(StatusCodes.Status204NoContent);

        var tiers = app.MapGroup("/api/admin/loyalty/tier-thresholds").WithTags("Loyalty Admin - Tiers")
            .RequireAuthorization(Permissions.LoyaltyRulesManage);

        tiers.MapGet("/", GetTierThresholdsAsync).WithName("GetTierThresholdsAdmin").Produces<IReadOnlyCollection<LoyaltyTierThresholdResponse>>(StatusCodes.Status200OK);
        tiers.MapPut("/{tier}", UpdateTierThresholdAsync).WithName("UpdateTierThreshold").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest);

        var rewards = app.MapGroup("/api/admin/loyalty/rewards").WithTags("Loyalty Admin - Rewards")
            .RequireAuthorization(Permissions.LoyaltyCatalogManage);

        rewards.MapGet("/", GetRewardCatalogAdminAsync).WithName("GetRewardCatalogAdmin").Produces<IReadOnlyCollection<LoyaltyRewardResponse>>(StatusCodes.Status200OK);
        rewards.MapPost("/", CreateRewardAsync).WithName("CreateReward").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);
        rewards.MapPut("/{rewardId:guid}", UpdateRewardAsync).WithName("UpdateReward").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);
        rewards.MapPost("/{rewardId:guid}/activate", ActivateRewardAsync).WithName("ActivateReward").Produces(StatusCodes.Status204NoContent);
        rewards.MapPost("/{rewardId:guid}/deactivate", DeactivateRewardAsync).WithName("DeactivateReward").Produces(StatusCodes.Status204NoContent);

        var challenges = app.MapGroup("/api/admin/loyalty/challenges").WithTags("Loyalty Admin - Challenges")
            .RequireAuthorization(Permissions.LoyaltyCatalogManage);

        challenges.MapGet("/", GetChallengesAdminAsync).WithName("GetChallengesAdmin").Produces<IReadOnlyCollection<LoyaltyChallengeResponse>>(StatusCodes.Status200OK);
        challenges.MapPost("/", CreateChallengeAsync).WithName("CreateChallenge").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);
        challenges.MapPost("/{challengeId:guid}/activate", ActivateChallengeAsync).WithName("ActivateChallenge").Produces(StatusCodes.Status204NoContent);
        challenges.MapPost("/{challengeId:guid}/deactivate", DeactivateChallengeAsync).WithName("DeactivateChallenge").Produces(StatusCodes.Status204NoContent);

        var adjustments = app.MapGroup("/api/admin/loyalty/adjustments").WithTags("Loyalty Admin - Adjustments")
            .RequireAuthorization(Permissions.LoyaltyAdjustmentsManage);

        adjustments.MapPost("/", AdjustPointsAsync).WithName("AdjustPoints").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var operations = app.MapGroup("/api/admin/loyalty/operations").WithTags("Loyalty Admin - Operations")
            .RequireAuthorization(Permissions.LoyaltyAdjustmentsManage);

        operations.MapPost("/process-expired-points", ProcessExpiredPointsAsync).WithName("ProcessExpiredPoints").Produces<int>(StatusCodes.Status200OK);
        operations.MapPost("/process-referral-rewards", ProcessReferralRewardsAsync).WithName("ProcessReferralRewards").Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    // --- Earning rules ---

    private static async Task<Ok<IReadOnlyCollection<LoyaltyEarningRuleResponse>>> GetEarningRulesAsync(
        GetEarningRulesAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var rules = await handler.Handle(new GetEarningRulesAdminQuery(), cancellationToken);
        IReadOnlyCollection<LoyaltyEarningRuleResponse> response = rules.Select(LoyaltyEarningRuleResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreateEarningRuleAsync(
        CreateEarningRuleRequest request, CreateEarningRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(request.ActorRole, ignoreCase: true, out var role))
        {
            return TypedResults.BadRequest(InvalidRoleError);
        }

        var result = await handler.Handle(
            new CreateEarningRuleCommand(
                request.Code, role, request.SourceType, request.RewardPointsPerCurrencyUnit, request.StatusPointsPerCurrencyUnit,
                request.MinPoints, request.MaxPoints, request.SubscriptionMultiplierAllowed, request.ValidFrom, request.ValidTo),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateEarningRuleAsync(
        Guid ruleId, UpdateEarningRuleRequest request, UpdateEarningRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateEarningRuleCommand(
                ruleId, request.RewardPointsPerCurrencyUnit, request.StatusPointsPerCurrencyUnit, request.MinPoints, request.MaxPoints,
                request.SubscriptionMultiplierAllowed, request.ValidFrom, request.ValidTo),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<NoContent> ActivateEarningRuleAsync(Guid ruleId, ActivateEarningRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new ActivateEarningRuleCommand(ruleId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeactivateEarningRuleAsync(Guid ruleId, DeactivateEarningRuleCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new DeactivateEarningRuleCommand(ruleId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Tier thresholds ---

    private static async Task<Ok<IReadOnlyCollection<LoyaltyTierThresholdResponse>>> GetTierThresholdsAsync(
        GetTierThresholdsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var thresholds = await handler.Handle(new GetTierThresholdsAdminQuery(), cancellationToken);
        IReadOnlyCollection<LoyaltyTierThresholdResponse> response = thresholds.Select(LoyaltyTierThresholdResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, BadRequest<string>, ProblemHttpResult>> UpdateTierThresholdAsync(
        string tier, UpdateTierThresholdRequest request, UpdateTierThresholdCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LoyaltyTier>(tier, ignoreCase: true, out var parsedTier))
        {
            return TypedResults.BadRequest(InvalidTierError);
        }

        var result = await handler.Handle(new UpdateTierThresholdCommand(parsedTier, request.MinimumStatusPoints), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    // --- Reward catalog ---

    private static async Task<Ok<IReadOnlyCollection<LoyaltyRewardResponse>>> GetRewardCatalogAdminAsync(
        GetRewardCatalogAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var rewards = await handler.Handle(new GetRewardCatalogAdminQuery(), cancellationToken);
        IReadOnlyCollection<LoyaltyRewardResponse> response = rewards.Select(LoyaltyRewardResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreateRewardAsync(
        CreateRewardRequest request, CreateRewardCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LoyaltyRewardType>(request.RewardType, ignoreCase: true, out var rewardType))
        {
            return TypedResults.BadRequest(InvalidRewardTypeError);
        }

        var targetRoles = new List<UserRole>();

        foreach (var roleName in request.TargetRoles)
        {
            if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var role))
            {
                return TypedResults.BadRequest(InvalidRoleError);
            }

            targetRoles.Add(role);
        }

        var result = await handler.Handle(
            new CreateRewardCommand(
                request.Code, request.Name, request.Description, request.CostInRewardPoints, rewardType, targetRoles,
                request.AvailableFrom, request.AvailableTo, request.UsageLimit),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateRewardAsync(
        Guid rewardId, UpdateRewardRequest request, UpdateRewardCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateRewardCommand(rewardId, request.Name, request.Description, request.CostInRewardPoints, request.AvailableFrom, request.AvailableTo, request.UsageLimit),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<NoContent> ActivateRewardAsync(Guid rewardId, ActivateRewardCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new ActivateRewardCommand(rewardId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeactivateRewardAsync(Guid rewardId, DeactivateRewardCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new DeactivateRewardCommand(rewardId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Challenges ---

    private static async Task<Ok<IReadOnlyCollection<LoyaltyChallengeResponse>>> GetChallengesAdminAsync(
        GetChallengesAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var challenges = await handler.Handle(new GetChallengesAdminQuery(), cancellationToken);
        IReadOnlyCollection<LoyaltyChallengeResponse> response = challenges.Select(LoyaltyChallengeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreateChallengeAsync(
        CreateChallengeRequest request, CreateChallengeCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LoyaltyChallengeCriteriaType>(request.CriteriaType, ignoreCase: true, out var criteriaType))
        {
            return TypedResults.BadRequest(InvalidCriteriaError);
        }

        UserRole? eligibleRole = null;

        if (!string.IsNullOrWhiteSpace(request.EligibleRole))
        {
            if (!Enum.TryParse<UserRole>(request.EligibleRole, ignoreCase: true, out var role))
            {
                return TypedResults.BadRequest(InvalidRoleError);
            }

            eligibleRole = role;
        }

        var result = await handler.Handle(
            new CreateChallengeCommand(
                request.Code, request.Name, criteriaType, request.TargetValue, request.RewardPoints, eligibleRole, request.ValidFrom, request.ValidTo),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<NoContent> ActivateChallengeAsync(Guid challengeId, ActivateChallengeCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new ActivateChallengeCommand(challengeId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeactivateChallengeAsync(Guid challengeId, DeactivateChallengeCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new DeactivateChallengeCommand(challengeId), cancellationToken);
        return TypedResults.NoContent();
    }

    // --- Adjustments & operations ---

    private static async Task<Results<NoContent, BadRequest<string>, ProblemHttpResult>> AdjustPointsAsync(
        AdjustPointsRequest request, ClaimsPrincipal currentUser, AdjustPointsCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LoyaltyPointType>(request.PointType, ignoreCase: true, out var pointType))
        {
            return TypedResults.BadRequest(InvalidPointTypeError);
        }

        var adminId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new AdjustPointsCommand(adminId, request.UserId, pointType, request.Points, request.Reason), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<int>> ProcessExpiredPointsAsync(ProcessExpiredPointsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessExpiredPointsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<int>> ProcessReferralRewardsAsync(ProcessReferralRewardsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessReferralRewardsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
