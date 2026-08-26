using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Advertising;
using SmartTaxi.Application.Advertising.Commands.ActivatePlacement;
using SmartTaxi.Application.Advertising.Commands.ApproveCampaign;
using SmartTaxi.Application.Advertising.Commands.CreatePlacement;
using SmartTaxi.Application.Advertising.Commands.DeactivatePlacement;
using SmartTaxi.Application.Advertising.Commands.ProcessCompletedCampaigns;
using SmartTaxi.Application.Advertising.Commands.ProcessScheduledCampaigns;
using SmartTaxi.Application.Advertising.Commands.ReactivateCampaign;
using SmartTaxi.Application.Advertising.Commands.RejectCampaign;
using SmartTaxi.Application.Advertising.Commands.RequestCampaignChanges;
using SmartTaxi.Application.Advertising.Commands.SettleCampaignBudget;
using SmartTaxi.Application.Advertising.Commands.SuspendCampaign;
using SmartTaxi.Application.Advertising.Commands.UpdatePlacement;
using SmartTaxi.Application.Advertising.Queries.GetCampaignDetailsAdmin;
using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformanceAdmin;
using SmartTaxi.Application.Advertising.Queries.GetPendingReviewCampaigns;
using SmartTaxi.Application.Advertising.Queries.GetPlacementsAdmin;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.API.Endpoints.Advertising;

public static class AdvertisingAdminEndpoints
{
    private const int DefaultPageSize = 20;
    private const string InvalidMediaTypeError = "Type de média inconnu.";

    public static IEndpointRouteBuilder MapAdvertisingAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var campaigns = app.MapGroup("/api/admin/advertising/campaigns").WithTags("Advertising Admin - Campaigns");

        campaigns.MapGet("/pending", GetPendingReviewAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("GetPendingReviewCampaigns").Produces<PagedResult<AdCampaignResponse>>(StatusCodes.Status200OK);

        campaigns.MapGet("/{campaignId:guid}", GetCampaignDetailsAdminAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReadAll)
            .WithName("GetCampaignDetailsAdmin").Produces<CampaignDetailsResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        campaigns.MapGet("/{campaignId:guid}/performance", GetCampaignPerformanceAdminAsync).RequireAuthorization(Permissions.AdvertisingPerformanceReadAll)
            .WithName("GetCampaignPerformanceAdmin").Produces<CampaignPerformanceResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        campaigns.MapPost("/{campaignId:guid}/approve", ApproveCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("ApproveCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/reject", RejectCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("RejectCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/request-changes", RequestChangesAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("RequestCampaignChanges").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/suspend", SuspendCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("SuspendCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/reactivate", ReactivateCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("ReactivateCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/settle", SettleCampaignBudgetAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReview)
            .WithName("SettleCampaignBudget").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var placements = app.MapGroup("/api/admin/advertising/placements").WithTags("Advertising Admin - Placements")
            .RequireAuthorization(Permissions.AdvertisingPlacementsManage);

        placements.MapGet("/", GetPlacementsAdminAsync).WithName("GetPlacementsAdmin").Produces<IReadOnlyCollection<AdvertisingPlacementResponse>>(StatusCodes.Status200OK);
        placements.MapPost("/", CreatePlacementAsync).WithName("CreatePlacement").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);
        placements.MapPut("/{placementId:guid}", UpdatePlacementAsync).WithName("UpdatePlacement").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);
        placements.MapPost("/{placementId:guid}/activate", ActivatePlacementAsync).WithName("ActivatePlacement").Produces(StatusCodes.Status204NoContent);
        placements.MapPost("/{placementId:guid}/deactivate", DeactivatePlacementAsync).WithName("DeactivatePlacement").Produces(StatusCodes.Status204NoContent);

        var operations = app.MapGroup("/api/admin/advertising/operations").WithTags("Advertising Admin - Operations")
            .RequireAuthorization(Permissions.AdvertisingCampaignsReview);

        operations.MapPost("/process-scheduled-campaigns", ProcessScheduledCampaignsAsync).WithName("ProcessScheduledCampaigns").Produces<int>(StatusCodes.Status200OK);
        operations.MapPost("/process-completed-campaigns", ProcessCompletedCampaignsAsync).WithName("ProcessCompletedCampaigns").Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<PagedResult<AdCampaignResponse>>> GetPendingReviewAsync(
        GetPendingReviewCampaignsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetPendingReviewCampaignsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<AdCampaignResponse>(
            result.Items.Select(AdCampaignResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<CampaignDetailsResponse>, NotFound>> GetCampaignDetailsAdminAsync(
        Guid campaignId, GetCampaignDetailsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var details = await handler.Handle(new GetCampaignDetailsAdminQuery(campaignId), cancellationToken);

        if (details is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CampaignDetailsResponse(
            AdCampaignResponse.FromEntity(details.Campaign), details.Creatives.Select(CampaignCreativeResponse.FromEntity).ToList(),
            details.ReviewHistory.Select(CampaignReviewHistoryEntryResponse.FromEntity).ToList()));
    }

    private static async Task<Results<Ok<CampaignPerformanceResponse>, NotFound>> GetCampaignPerformanceAdminAsync(
        Guid campaignId, GetCampaignPerformanceAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var summary = await handler.Handle(new GetCampaignPerformanceAdminQuery(campaignId), cancellationToken);

        if (summary is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CampaignPerformanceResponse(summary.CampaignId, summary.Impressions, summary.Clicks, summary.Ctr, summary.BudgetLimit, summary.ConsumedBudget, summary.RemainingBudget));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ApproveCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, ApproveCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ApproveCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectCampaignAsync(
        Guid campaignId, RejectCampaignRequest request, ClaimsPrincipal currentUser, RejectCampaignCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectCampaignCommand(campaignId, CurrentUserId(currentUser), request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RequestChangesAsync(
        Guid campaignId, RequestCampaignChangesRequest request, ClaimsPrincipal currentUser, RequestCampaignChangesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RequestCampaignChangesCommand(campaignId, CurrentUserId(currentUser), request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SuspendCampaignAsync(
        Guid campaignId, SuspendCampaignRequest request, ClaimsPrincipal currentUser, SuspendCampaignCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SuspendCampaignCommand(campaignId, CurrentUserId(currentUser), request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReactivateCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, ReactivateCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReactivateCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SettleCampaignBudgetAsync(
        Guid campaignId, ClaimsPrincipal currentUser, SettleCampaignBudgetCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SettleCampaignBudgetCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<AdvertisingPlacementResponse>>> GetPlacementsAdminAsync(
        GetPlacementsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var placements = await handler.Handle(new GetPlacementsAdminQuery(), cancellationToken);
        IReadOnlyCollection<AdvertisingPlacementResponse> response = placements.Select(AdvertisingPlacementResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreatePlacementAsync(
        CreatePlacementRequest request, CreatePlacementCommandHandler handler, CancellationToken cancellationToken)
    {
        var mediaTypes = new List<AdMediaType>();

        foreach (var typeName in request.SupportedMediaTypes)
        {
            if (!Enum.TryParse<AdMediaType>(typeName, ignoreCase: true, out var mediaType))
            {
                return TypedResults.BadRequest(InvalidMediaTypeError);
            }

            mediaTypes.Add(mediaType);
        }

        var result = await handler.Handle(new CreatePlacementCommand(request.Code, request.Name, request.Description, mediaTypes), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, BadRequest<string>, ProblemHttpResult>> UpdatePlacementAsync(
        Guid placementId, UpdatePlacementRequest request, UpdatePlacementCommandHandler handler, CancellationToken cancellationToken)
    {
        var mediaTypes = new List<AdMediaType>();

        foreach (var typeName in request.SupportedMediaTypes)
        {
            if (!Enum.TryParse<AdMediaType>(typeName, ignoreCase: true, out var mediaType))
            {
                return TypedResults.BadRequest(InvalidMediaTypeError);
            }

            mediaTypes.Add(mediaType);
        }

        var result = await handler.Handle(new UpdatePlacementCommand(placementId, request.Name, request.Description, mediaTypes), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<NoContent> ActivatePlacementAsync(Guid placementId, ActivatePlacementCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new ActivatePlacementCommand(placementId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> DeactivatePlacementAsync(Guid placementId, DeactivatePlacementCommandHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(new DeactivatePlacementCommand(placementId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<int>> ProcessScheduledCampaignsAsync(ProcessScheduledCampaignsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessScheduledCampaignsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<int>> ProcessCompletedCampaignsAsync(ProcessCompletedCampaignsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProcessCompletedCampaignsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
