using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Maintenance;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Maintenance.Commands.CancelMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.CompleteMaintenance;
using SmartTaxi.Application.Maintenance.Commands.CreateMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.MarkVehicleReceived;
using SmartTaxi.Application.Maintenance.Commands.MarkWaitingForParts;
using SmartTaxi.Application.Maintenance.Commands.RegisterGarageProfile;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceQuote;
using SmartTaxi.Application.Maintenance.Commands.RespondToMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.ResumeMaintenanceWork;
using SmartTaxi.Application.Maintenance.Commands.StartMaintenanceWork;
using SmartTaxi.Application.Maintenance.Commands.SubmitMaintenanceQuote;
using SmartTaxi.Application.Maintenance.Commands.UpdateGarageProfile;
using SmartTaxi.Application.Maintenance.Queries.GetMaintenanceRequestDetails;
using SmartTaxi.Application.Maintenance.Queries.GetMyGarageJobs;
using SmartTaxi.Application.Maintenance.Queries.GetMyGarageProfile;
using SmartTaxi.Application.Maintenance.Queries.GetMyMaintenanceRequests;
using SmartTaxi.Application.Maintenance.Queries.GetMyVehicleMaintenanceHistory;

namespace SmartTaxi.API.Endpoints.Maintenance;

/// <summary>
/// Self-service — every read/write here is scoped to the caller's own JWT
/// subject claim (never a client-supplied owner/garage id in the body), same
/// convention as Advertising/Loyalty's self-service endpoints. Owner and
/// Garage groups are separated by their own distinct permissions.
/// </summary>
public static class MaintenanceEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder app)
    {
        var garageProfile = app.MapGroup("/api/maintenance/garage-profile").WithTags("Maintenance - Garage Profile")
            .RequireAuthorization(Permissions.MaintenanceGarageProfileManageOwn);

        garageProfile.MapPost("/", RegisterGarageProfileAsync).WithName("RegisterGarageProfile")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        garageProfile.MapGet("/", GetMyGarageProfileAsync).WithName("GetMyGarageProfile")
            .Produces<GarageProfileResponse?>(StatusCodes.Status200OK);

        garageProfile.MapPut("/", UpdateGarageProfileAsync).WithName("UpdateGarageProfile")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        var requests = app.MapGroup("/api/maintenance/requests").WithTags("Maintenance - Owner Requests");

        requests.MapPost("/", CreateMaintenanceRequestAsync).RequireAuthorization(Permissions.MaintenanceRequestsCreateOwn)
            .WithName("CreateMaintenanceRequest").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapGet("/", GetMyMaintenanceRequestsAsync).RequireAuthorization(Permissions.MaintenanceRequestsReadOwn)
            .WithName("GetMyMaintenanceRequests").Produces<PagedResult<MaintenanceRequestResponse>>(StatusCodes.Status200OK);

        // Mapped once per group (same handler, same underlying ownership-or-garage check inside
        // GetMaintenanceRequestDetailsQueryHandler) rather than a single route gated by both permissions —
        // ASP.NET Core's RequireAuthorization ANDs multiple policy names together, which would incorrectly
        // require a caller to hold both simultaneously instead of either one.
        requests.MapGet("/{requestId:guid}", GetMaintenanceRequestDetailsAsync).RequireAuthorization(Permissions.MaintenanceRequestsReadOwn)
            .WithName("GetMaintenanceRequestDetails").Produces<MaintenanceRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/{requestId:guid}/cancel", CancelMaintenanceRequestAsync).RequireAuthorization(Permissions.MaintenanceRequestsManageOwn)
            .WithName("CancelMaintenanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/quote/respond", RespondToMaintenanceQuoteAsync).RequireAuthorization(Permissions.MaintenanceRequestsManageOwn)
            .WithName("RespondToMaintenanceQuote").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet("/api/maintenance/vehicles/{vehicleId:guid}/history", GetMyVehicleMaintenanceHistoryAsync)
            .RequireAuthorization(Permissions.MaintenanceRecordsReadOwn).WithTags("Maintenance - Owner Requests").WithName("GetMyVehicleMaintenanceHistory")
            .Produces<IReadOnlyCollection<MaintenanceRecordResponse>>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        var garageJobs = app.MapGroup("/api/maintenance/garage/jobs").WithTags("Maintenance - Garage Jobs")
            .RequireAuthorization(Permissions.MaintenanceJobsManageOwn);

        garageJobs.MapGet("/", GetMyGarageJobsAsync).WithName("GetMyGarageJobs").Produces<PagedResult<MaintenanceRequestResponse>>(StatusCodes.Status200OK);

        garageJobs.MapGet("/{requestId:guid}", GetMaintenanceRequestDetailsAsync).WithName("GetGarageJobDetails")
            .Produces<MaintenanceRequestResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        garageJobs.MapPost("/{requestId:guid}/respond", RespondToMaintenanceRequestAsync).WithName("RespondToMaintenanceRequest")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/quote", SubmitMaintenanceQuoteAsync).WithName("SubmitMaintenanceQuote")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/vehicle-received", MarkVehicleReceivedAsync).WithName("MarkVehicleReceived")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/start", StartMaintenanceWorkAsync).WithName("StartMaintenanceWork")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/waiting-for-parts", MarkWaitingForPartsAsync).WithName("MarkWaitingForParts")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/resume", ResumeMaintenanceWorkAsync).WithName("ResumeMaintenanceWork")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        garageJobs.MapPost("/{requestId:guid}/complete", CompleteMaintenanceAsync).WithName("CompleteMaintenance")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RegisterGarageProfileAsync(
        RegisterGarageProfileRequest request, ClaimsPrincipal currentUser, RegisterGarageProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterGarageProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.Address, request.City,
                request.SupportedVehicleCategories, request.AvailableServices),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<GarageProfileResponse?>> GetMyGarageProfileAsync(
        ClaimsPrincipal currentUser, GetMyGarageProfileQueryHandler handler, CancellationToken cancellationToken)
    {
        var profile = await handler.Handle(new GetMyGarageProfileQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(profile is null ? null : GarageProfileResponse.FromEntity(profile));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateGarageProfileAsync(
        UpdateGarageProfileRequest request, ClaimsPrincipal currentUser, UpdateGarageProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateGarageProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.Address, request.City,
                request.SupportedVehicleCategories, request.AvailableServices),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateMaintenanceRequestAsync(
        CreateMaintenanceRequestRequest request, ClaimsPrincipal currentUser, CreateMaintenanceRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateMaintenanceRequestCommand(CurrentUserId(currentUser), request.VehicleId, request.GarageUserId, request.Description),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<MaintenanceRequestResponse>>> GetMyMaintenanceRequestsAsync(
        ClaimsPrincipal currentUser, GetMyMaintenanceRequestsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyMaintenanceRequestsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<MaintenanceRequestResponse>(
            result.Items.Select(MaintenanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<MaintenanceRequestResponse>, ProblemHttpResult>> GetMaintenanceRequestDetailsAsync(
        Guid requestId, ClaimsPrincipal currentUser, GetMaintenanceRequestDetailsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMaintenanceRequestDetailsQuery(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(MaintenanceRequestResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelMaintenanceRequestAsync(
        Guid requestId, CancelMaintenanceRequestRequest request, ClaimsPrincipal currentUser, CancelMaintenanceRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelMaintenanceRequestCommand(requestId, CurrentUserId(currentUser), request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RespondToMaintenanceQuoteAsync(
        Guid requestId, RespondToMaintenanceQuoteRequest request, ClaimsPrincipal currentUser, RespondToMaintenanceQuoteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RespondToMaintenanceQuoteCommand(requestId, CurrentUserId(currentUser), request.IsAccepted), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<MaintenanceRecordResponse>>, ProblemHttpResult>> GetMyVehicleMaintenanceHistoryAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, GetMyVehicleMaintenanceHistoryQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMyVehicleMaintenanceHistoryQuery(vehicleId, CurrentUserId(currentUser)), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<MaintenanceRecordResponse> response = result.Value!.Select(MaintenanceRecordResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<PagedResult<MaintenanceRequestResponse>>> GetMyGarageJobsAsync(
        ClaimsPrincipal currentUser, GetMyGarageJobsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyGarageJobsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<MaintenanceRequestResponse>(
            result.Items.Select(MaintenanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RespondToMaintenanceRequestAsync(
        Guid requestId, RespondToMaintenanceRequestRequest request, ClaimsPrincipal currentUser, RespondToMaintenanceRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RespondToMaintenanceRequestCommand(requestId, CurrentUserId(currentUser), request.IsAccepted, request.RejectionReason),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SubmitMaintenanceQuoteAsync(
        Guid requestId, SubmitMaintenanceQuoteRequest request, ClaimsPrincipal currentUser, SubmitMaintenanceQuoteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SubmitMaintenanceQuoteCommand(requestId, CurrentUserId(currentUser), request.EstimatedCost), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkVehicleReceivedAsync(
        Guid requestId, ClaimsPrincipal currentUser, MarkVehicleReceivedCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkVehicleReceivedCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartMaintenanceWorkAsync(
        Guid requestId, ClaimsPrincipal currentUser, StartMaintenanceWorkCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartMaintenanceWorkCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkWaitingForPartsAsync(
        Guid requestId, ClaimsPrincipal currentUser, MarkWaitingForPartsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkWaitingForPartsCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResumeMaintenanceWorkAsync(
        Guid requestId, ClaimsPrincipal currentUser, ResumeMaintenanceWorkCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResumeMaintenanceWorkCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CompleteMaintenanceAsync(
        Guid requestId, CompleteMaintenanceRequest request, ClaimsPrincipal currentUser, CompleteMaintenanceCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var lines = request.Lines
            .Select(line => new MaintenanceRecordLineInput(line.Description, line.IsPart, line.Quantity, line.UnitCost)).ToList();

        var result = await handler.Handle(
            new CompleteMaintenanceCommand(
                requestId, CurrentUserId(currentUser), request.FinalCost, lines, request.Notes, request.NextRecommendedServiceDate,
                request.WarrantyInfo),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
