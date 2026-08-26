using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Advertising;
using SmartTaxi.Application.Advertising.Commands.CancelCampaign;
using SmartTaxi.Application.Advertising.Commands.CreateCampaign;
using SmartTaxi.Application.Advertising.Commands.PauseCampaign;
using SmartTaxi.Application.Advertising.Commands.RecordClick;
using SmartTaxi.Application.Advertising.Commands.RecordImpression;
using SmartTaxi.Application.Advertising.Commands.RegisterAdvertiserProfile;
using SmartTaxi.Application.Advertising.Commands.RequestAdDelivery;
using SmartTaxi.Application.Advertising.Commands.ResumeCampaign;
using SmartTaxi.Application.Advertising.Commands.SubmitCampaign;
using SmartTaxi.Application.Advertising.Commands.UpdateAdvertiserProfile;
using SmartTaxi.Application.Advertising.Commands.UpdateCampaign;
using SmartTaxi.Application.Advertising.Commands.UploadCampaignMedia;
using SmartTaxi.Application.Advertising.Queries.GetCampaignDetails;
using SmartTaxi.Application.Advertising.Queries.GetCampaignMediaContent;
using SmartTaxi.Application.Advertising.Queries.GetCampaignPerformance;
using SmartTaxi.Application.Advertising.Queries.GetMyAdvertiserProfile;
using SmartTaxi.Application.Advertising.Queries.GetMyCampaigns;
using SmartTaxi.Application.Advertising.Queries.GetPlacements;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Advertising.Enums;

namespace SmartTaxi.API.Endpoints.Advertising;

/// <summary>
/// Self-service — every read/write here is scoped to the caller's own JWT
/// subject claim (never a client-supplied owner id in the body), same
/// convention as Loyalty's self-service endpoints. Tracking endpoints
/// (impressions/clicks) are the one exception: they represent a consumer-side
/// app (rider/driver) viewing a served ad, not the advertiser themselves —
/// see Permissions.AdvertisingTrackingRecord.
/// </summary>
public static class AdvertisingEndpoints
{
    private const int DefaultPageSize = 20;
    private const string InvalidPricingModelError = "Modèle de tarification inconnu.";

    public static IEndpointRouteBuilder MapAdvertisingEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/api/advertising/profile").WithTags("Advertising - Profile");

        profile.MapPost("/", RegisterProfileAsync).RequireAuthorization(Permissions.AdvertisingProfileManageOwn)
            .WithName("RegisterAdvertiserProfile").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        profile.MapGet("/", GetMyProfileAsync).RequireAuthorization(Permissions.AdvertisingProfileManageOwn)
            .WithName("GetMyAdvertiserProfile").Produces<AdvertiserProfileResponse?>(StatusCodes.Status200OK);

        profile.MapPut("/", UpdateProfileAsync).RequireAuthorization(Permissions.AdvertisingProfileManageOwn)
            .WithName("UpdateAdvertiserProfile").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        var campaigns = app.MapGroup("/api/advertising/campaigns").WithTags("Advertising - Campaigns");

        campaigns.MapPost("/", CreateCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("CreateCampaign").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        campaigns.MapGet("/", GetMyCampaignsAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReadOwn)
            .WithName("GetMyCampaigns").Produces<PagedResult<AdCampaignResponse>>(StatusCodes.Status200OK);

        campaigns.MapGet("/{campaignId:guid}", GetCampaignDetailsAsync).RequireAuthorization(Permissions.AdvertisingCampaignsReadOwn)
            .WithName("GetMyCampaignDetails").Produces<CampaignDetailsResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        campaigns.MapPut("/{campaignId:guid}", UpdateCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("UpdateCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/submit", SubmitCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsSubmitOwn)
            .WithName("SubmitCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/cancel", CancelCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("CancelCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/pause", PauseCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("PauseCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/resume", ResumeCampaignAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("ResumeCampaign").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        campaigns.MapPost("/{campaignId:guid}/media", UploadMediaAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithName("UploadCampaignMedia").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        campaigns.MapGet("/{campaignId:guid}/media/{creativeId:guid}/content", DownloadMediaContentAsync)
            .RequireAuthorization(Permissions.AdvertisingCampaignsReadOwn).WithName("DownloadCampaignMediaContent")
            .ProducesProblem(StatusCodes.Status404NotFound);

        campaigns.MapGet("/{campaignId:guid}/performance", GetCampaignPerformanceAsync).RequireAuthorization(Permissions.AdvertisingPerformanceReadOwn)
            .WithName("GetMyCampaignPerformance").Produces<CampaignPerformanceResponse>(StatusCodes.Status200OK);

        app.MapGet("/api/advertising/placements", GetPlacementsAsync).RequireAuthorization(Permissions.AdvertisingCampaignsManageOwn)
            .WithTags("Advertising - Placements").WithName("GetPlacements").Produces<IReadOnlyCollection<AdvertisingPlacementResponse>>(StatusCodes.Status200OK);

        var tracking = app.MapGroup("/api/advertising/tracking").WithTags("Advertising - Tracking").RequireAuthorization(Permissions.AdvertisingTrackingRecord);

        tracking.MapPost("/campaigns/{campaignId:guid}/delivery-request", RequestAdDeliveryAsync)
            .WithName("RequestAdDelivery").Produces<AdDeliveryTokenResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        tracking.MapPost("/campaigns/{campaignId:guid}/impressions", RecordImpressionAsync)
            .WithName("RecordAdImpression").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        tracking.MapPost("/campaigns/{campaignId:guid}/clicks", RecordClickAsync)
            .WithName("RecordAdClick").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RegisterProfileAsync(
        RegisterAdvertiserProfileRequest request, ClaimsPrincipal currentUser, RegisterAdvertiserProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterAdvertiserProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.TaxIdentifier, request.City, request.Address,
                request.ContactEmail, request.ContactPhone),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<AdvertiserProfileResponse?>> GetMyProfileAsync(
        ClaimsPrincipal currentUser, GetMyAdvertiserProfileQueryHandler handler, CancellationToken cancellationToken)
    {
        var profile = await handler.Handle(new GetMyAdvertiserProfileQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(profile is null ? null : AdvertiserProfileResponse.FromEntity(profile));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateProfileAsync(
        UpdateAdvertiserProfileRequest request, ClaimsPrincipal currentUser, UpdateAdvertiserProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateAdvertiserProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.TaxIdentifier, request.City, request.Address,
                request.ContactEmail, request.ContactPhone),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>, ProblemHttpResult>> CreateCampaignAsync(
        CreateCampaignRequest request, ClaimsPrincipal currentUser, CreateCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AdPricingModel>(request.PricingModel, ignoreCase: true, out var pricingModel))
        {
            return TypedResults.BadRequest(InvalidPricingModelError);
        }

        var result = await handler.Handle(
            new CreateCampaignCommand(
                CurrentUserId(currentUser), request.Name, request.Description, request.Objective, request.PlacementId, request.StartAtUtc,
                request.EndAtUtc, pricingModel, request.PriceRate, request.BudgetLimit, request.DailyBudgetLimit, request.TargetCity,
                request.TargetVehicleCategory, request.TargetDaysOfWeek, request.TargetStartHour, request.TargetEndHour),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<AdCampaignResponse>>> GetMyCampaignsAsync(
        ClaimsPrincipal currentUser, GetMyCampaignsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyCampaignsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<AdCampaignResponse>(
            result.Items.Select(AdCampaignResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<CampaignDetailsResponse>, ProblemHttpResult>> GetCampaignDetailsAsync(
        Guid campaignId, ClaimsPrincipal currentUser, GetCampaignDetailsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCampaignDetailsQuery(campaignId, CurrentUserId(currentUser)), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var details = result.Value!;
        return TypedResults.Ok(new CampaignDetailsResponse(
            AdCampaignResponse.FromEntity(details.Campaign), details.Creatives.Select(CampaignCreativeResponse.FromEntity).ToList(),
            details.ReviewHistory.Select(CampaignReviewHistoryEntryResponse.FromEntity).ToList()));
    }

    private static async Task<Results<NoContent, BadRequest<string>, ProblemHttpResult>> UpdateCampaignAsync(
        Guid campaignId, UpdateCampaignRequest request, ClaimsPrincipal currentUser, UpdateCampaignCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AdPricingModel>(request.PricingModel, ignoreCase: true, out var pricingModel))
        {
            return TypedResults.BadRequest(InvalidPricingModelError);
        }

        var result = await handler.Handle(
            new UpdateCampaignCommand(
                campaignId, CurrentUserId(currentUser), request.Name, request.Description, request.Objective, request.PlacementId,
                request.StartAtUtc, request.EndAtUtc, pricingModel, request.PriceRate, request.BudgetLimit, request.DailyBudgetLimit,
                request.TargetCity, request.TargetVehicleCategory, request.TargetDaysOfWeek, request.TargetStartHour, request.TargetEndHour),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SubmitCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, SubmitCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SubmitCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, CancelCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> PauseCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, PauseCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new PauseCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResumeCampaignAsync(
        Guid campaignId, ClaimsPrincipal currentUser, ResumeCampaignCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResumeCampaignCommand(campaignId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> UploadMediaAsync(
        Guid campaignId, IFormFile file, [FromForm] string mediaType, ClaimsPrincipal currentUser, UploadCampaignMediaCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AdMediaType>(mediaType, ignoreCase: true, out var parsedMediaType))
        {
            return TypedResults.Problem("Type de média inconnu.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        var result = await handler.Handle(
            new UploadCampaignMediaCommand(campaignId, CurrentUserId(currentUser), parsedMediaType, file.ContentType, file.FileName, stream),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> DownloadMediaContentAsync(
        Guid campaignId, Guid creativeId, ClaimsPrincipal currentUser, GetCampaignMediaContentQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCampaignMediaContentQuery(creativeId, CurrentUserId(currentUser)), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        return TypedResults.Stream(result.Value!.Content, result.Value.MimeType, result.Value.FileName);
    }

    private static async Task<Results<Ok<CampaignPerformanceResponse>, ProblemHttpResult>> GetCampaignPerformanceAsync(
        Guid campaignId, ClaimsPrincipal currentUser, GetCampaignPerformanceQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCampaignPerformanceQuery(campaignId, CurrentUserId(currentUser)), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var summary = result.Value!;
        return TypedResults.Ok(new CampaignPerformanceResponse(summary.CampaignId, summary.Impressions, summary.Clicks, summary.Ctr, summary.BudgetLimit, summary.ConsumedBudget, summary.RemainingBudget));
    }

    private static async Task<Ok<IReadOnlyCollection<AdvertisingPlacementResponse>>> GetPlacementsAsync(
        GetPlacementsQueryHandler handler, CancellationToken cancellationToken)
    {
        var placements = await handler.Handle(new GetPlacementsQuery(), cancellationToken);
        IReadOnlyCollection<AdvertisingPlacementResponse> response = placements.Select(AdvertisingPlacementResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<AdDeliveryTokenResponse>, ProblemHttpResult>> RequestAdDeliveryAsync(
        Guid campaignId, RequestAdDeliveryRequest request, ClaimsPrincipal currentUser, RequestAdDeliveryCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RequestAdDeliveryCommand(campaignId, request.PlacementId, CurrentUserId(currentUser)), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(new AdDeliveryTokenResponse(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RecordImpressionAsync(
        Guid campaignId, RecordImpressionRequest request, ClaimsPrincipal currentUser, RecordImpressionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RecordImpressionCommand(campaignId, request.PlacementId, CurrentUserId(currentUser), request.DeliveryToken, request.OccurredAtUtc),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RecordClickAsync(
        Guid campaignId, RecordClickRequest request, ClaimsPrincipal currentUser, RecordClickCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RecordClickCommand(
                campaignId, request.PlacementId, CurrentUserId(currentUser), request.ImpressionId, request.DeliveryToken, request.OccurredAtUtc),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
