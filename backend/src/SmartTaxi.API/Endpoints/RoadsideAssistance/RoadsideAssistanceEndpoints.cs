using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.RoadsideAssistance;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.RoadsideAssistance.Commands.AcceptRoadsideJob;
using SmartTaxi.Application.RoadsideAssistance.Commands.CancelRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.CompleteRoadsideIntervention;
using SmartTaxi.Application.RoadsideAssistance.Commands.CreateRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.EscalateRoadsideRequestToMaintenance;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerArrived;
using SmartTaxi.Application.RoadsideAssistance.Commands.MarkPartnerOnTheWay;
using SmartTaxi.Application.RoadsideAssistance.Commands.RegisterRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Commands.RejectRoadsideJob;
using SmartTaxi.Application.RoadsideAssistance.Commands.ReselectRoadsideRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.SelectRoadsidePartner;
using SmartTaxi.Application.RoadsideAssistance.Commands.StartRoadsideIntervention;
using SmartTaxi.Application.RoadsideAssistance.Commands.UpdateRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideAssistanceRequests;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsideJobs;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetMyRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRecommendedRoadsidePartners;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRoadsideAssistanceRequestById;

namespace SmartTaxi.API.Endpoints.RoadsideAssistance;

/// <summary>
/// Self-service — every read/write here is scoped to the caller's own JWT
/// subject claim (never a client-supplied requester/partner id in the body),
/// same convention as Maintenance/Advertising's self-service endpoints.
/// Requester and Partner groups are separated by their own distinct
/// permissions. GetRoadsideAssistanceRequestDetails is mapped once per group
/// (same handler, same underlying requester-or-partner check inside
/// GetRoadsideAssistanceRequestByIdQueryHandler) rather than a single route
/// gated by both permissions — RequireAuthorization ANDs multiple policy
/// names together, which would incorrectly require holding both at once.
/// </summary>
public static class RoadsideAssistanceEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapRoadsideAssistanceEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/roadside/requests").WithTags("Roadside Assistance - Requester");

        requests.MapPost("/", CreateRoadsideAssistanceRequestAsync).RequireAuthorization(Permissions.RoadsideRequestsCreateOwn)
            .WithName("CreateRoadsideAssistanceRequest").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapGet("/mine", GetMyRoadsideAssistanceRequestsAsync).RequireAuthorization(Permissions.RoadsideRequestsReadOwn)
            .WithName("GetMyRoadsideAssistanceRequests").Produces<PagedResult<RoadsideAssistanceRequestResponse>>(StatusCodes.Status200OK);

        requests.MapGet("/{requestId:guid}", GetRoadsideAssistanceRequestDetailsAsync).RequireAuthorization(Permissions.RoadsideRequestsReadOwn)
            .WithName("GetRoadsideAssistanceRequestDetails").Produces<RoadsideAssistanceRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapGet("/{requestId:guid}/recommended-partners", GetRecommendedRoadsidePartnersAsync)
            .RequireAuthorization(Permissions.RoadsideRequestsReadOwn).WithName("GetRecommendedRoadsidePartners")
            .Produces<IReadOnlyCollection<RecommendedRoadsidePartnerResponse>>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/{requestId:guid}/select-partner", SelectRoadsidePartnerAsync).RequireAuthorization(Permissions.RoadsideRequestsManageOwn)
            .WithName("SelectRoadsidePartner").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/reselect", ReselectRoadsideRequestAsync).RequireAuthorization(Permissions.RoadsideRequestsManageOwn)
            .WithName("ReselectRoadsideRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/cancel", CancelRoadsideAssistanceRequestAsync).RequireAuthorization(Permissions.RoadsideRequestsManageOwn)
            .WithName("CancelRoadsideAssistanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/escalate-to-maintenance", EscalateRoadsideRequestToMaintenanceAsync)
            .RequireAuthorization(Permissions.RoadsideRequestsManageOwn).WithName("EscalateRoadsideRequestToMaintenance")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        var partnerProfile = app.MapGroup("/api/roadside/partner-profile").WithTags("Roadside Assistance - Partner Profile")
            .RequireAuthorization(Permissions.RoadsidePartnerProfileManageOwn);

        partnerProfile.MapPost("/", RegisterRoadsidePartnerProfileAsync).WithName("RegisterRoadsidePartnerProfile")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        partnerProfile.MapGet("/", GetMyRoadsidePartnerProfileAsync).WithName("GetMyRoadsidePartnerProfile")
            .Produces<RoadsidePartnerProfileResponse?>(StatusCodes.Status200OK);

        partnerProfile.MapPut("/", UpdateRoadsidePartnerProfileAsync).WithName("UpdateRoadsidePartnerProfile")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        var jobs = app.MapGroup("/api/roadside/jobs").WithTags("Roadside Assistance - Partner Jobs")
            .RequireAuthorization(Permissions.RoadsideJobsManageOwn);

        jobs.MapGet("/", GetMyRoadsideJobsAsync).WithName("GetMyRoadsideJobs").Produces<PagedResult<RoadsideAssistanceRequestResponse>>(StatusCodes.Status200OK);

        jobs.MapGet("/{requestId:guid}", GetRoadsideAssistanceRequestDetailsAsync).WithName("GetRoadsideJobDetails")
            .Produces<RoadsideAssistanceRequestResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        jobs.MapPost("/{requestId:guid}/accept", AcceptRoadsideJobAsync).WithName("AcceptRoadsideJob")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        jobs.MapPost("/{requestId:guid}/reject", RejectRoadsideJobAsync).WithName("RejectRoadsideJob")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        jobs.MapPost("/{requestId:guid}/en-route", MarkPartnerOnTheWayAsync).WithName("MarkPartnerOnTheWay")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        jobs.MapPost("/{requestId:guid}/arrived", MarkPartnerArrivedAsync).WithName("MarkPartnerArrived")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        jobs.MapPost("/{requestId:guid}/start", StartRoadsideInterventionAsync).WithName("StartRoadsideIntervention")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        jobs.MapPost("/{requestId:guid}/complete", CompleteRoadsideInterventionAsync).WithName("CompleteRoadsideIntervention")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateRoadsideAssistanceRequestAsync(
        CreateRoadsideAssistanceRequestRequest request, ClaimsPrincipal currentUser, CreateRoadsideAssistanceRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateRoadsideAssistanceRequestCommand(
                CurrentUserId(currentUser), request.RequesterRole, request.VehicleId, request.RideId, request.ServiceType, request.Urgency,
                request.Description, request.Latitude, request.Longitude, request.Address, request.City),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<RoadsideAssistanceRequestResponse>>> GetMyRoadsideAssistanceRequestsAsync(
        ClaimsPrincipal currentUser, GetMyRoadsideAssistanceRequestsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyRoadsideAssistanceRequestsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<RoadsideAssistanceRequestResponse>(
            result.Items.Select(RoadsideAssistanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<RoadsideAssistanceRequestResponse>, ProblemHttpResult>> GetRoadsideAssistanceRequestDetailsAsync(
        Guid requestId, ClaimsPrincipal currentUser, GetRoadsideAssistanceRequestByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRoadsideAssistanceRequestByIdQuery(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(RoadsideAssistanceRequestResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RecommendedRoadsidePartnerResponse>>, ProblemHttpResult>> GetRecommendedRoadsidePartnersAsync(
        Guid requestId, ClaimsPrincipal currentUser, GetRecommendedRoadsidePartnersQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRecommendedRoadsidePartnersQuery(requestId, CurrentUserId(currentUser)), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<RecommendedRoadsidePartnerResponse> response =
            result.Value!.Select(RecommendedRoadsidePartnerResponse.FromDto).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SelectRoadsidePartnerAsync(
        Guid requestId, SelectRoadsidePartnerRequest request, ClaimsPrincipal currentUser, SelectRoadsidePartnerCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SelectRoadsidePartnerCommand(requestId, CurrentUserId(currentUser), request.PartnerUserId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReselectRoadsideRequestAsync(
        Guid requestId, ClaimsPrincipal currentUser, ReselectRoadsideRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReselectRoadsideRequestCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelRoadsideAssistanceRequestAsync(
        Guid requestId, CancelRoadsideAssistanceRequestRequest request, ClaimsPrincipal currentUser,
        CancelRoadsideAssistanceRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CancelRoadsideAssistanceRequestCommand(requestId, CurrentUserId(currentUser), request.Reason), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> EscalateRoadsideRequestToMaintenanceAsync(
        Guid requestId, EscalateRoadsideRequestToMaintenanceRequest request, ClaimsPrincipal currentUser,
        EscalateRoadsideRequestToMaintenanceCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(requestId, CurrentUserId(currentUser), request.GarageUserId, request.Description),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RegisterRoadsidePartnerProfileAsync(
        RegisterRoadsidePartnerProfileRequest request, ClaimsPrincipal currentUser, RegisterRoadsidePartnerProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterRoadsidePartnerProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.Address, request.City,
                request.SupportedServiceTypes, request.SupportedVehicleCategories, request.Latitude, request.Longitude),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<RoadsidePartnerProfileResponse?>> GetMyRoadsidePartnerProfileAsync(
        ClaimsPrincipal currentUser, GetMyRoadsidePartnerProfileQueryHandler handler, CancellationToken cancellationToken)
    {
        var profile = await handler.Handle(new GetMyRoadsidePartnerProfileQuery(CurrentUserId(currentUser)), cancellationToken);
        return TypedResults.Ok(profile is null ? null : RoadsidePartnerProfileResponse.FromEntity(profile));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateRoadsidePartnerProfileAsync(
        UpdateRoadsidePartnerProfileRequest request, ClaimsPrincipal currentUser, UpdateRoadsidePartnerProfileCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateRoadsidePartnerProfileCommand(
                CurrentUserId(currentUser), request.BusinessName, request.LegalName, request.Address, request.City,
                request.SupportedServiceTypes, request.SupportedVehicleCategories, request.Latitude, request.Longitude),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<RoadsideAssistanceRequestResponse>>> GetMyRoadsideJobsAsync(
        ClaimsPrincipal currentUser, GetMyRoadsideJobsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMyRoadsideJobsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<RoadsideAssistanceRequestResponse>(
            result.Items.Select(RoadsideAssistanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AcceptRoadsideJobAsync(
        Guid requestId, AcceptRoadsideJobRequest request, ClaimsPrincipal currentUser, AcceptRoadsideJobCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AcceptRoadsideJobCommand(requestId, CurrentUserId(currentUser), request.EstimatedCost), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectRoadsideJobAsync(
        Guid requestId, RejectRoadsideJobRequest request, ClaimsPrincipal currentUser, RejectRoadsideJobCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RejectRoadsideJobCommand(requestId, CurrentUserId(currentUser), request.RejectionReason), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkPartnerOnTheWayAsync(
        Guid requestId, ClaimsPrincipal currentUser, MarkPartnerOnTheWayCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkPartnerOnTheWayCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkPartnerArrivedAsync(
        Guid requestId, ClaimsPrincipal currentUser, MarkPartnerArrivedCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkPartnerArrivedCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartRoadsideInterventionAsync(
        Guid requestId, ClaimsPrincipal currentUser, StartRoadsideInterventionCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartRoadsideInterventionCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CompleteRoadsideInterventionAsync(
        Guid requestId, CompleteRoadsideInterventionRequest request, ClaimsPrincipal currentUser, CompleteRoadsideInterventionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CompleteRoadsideInterventionCommand(requestId, CurrentUserId(currentUser), request.FinalCost), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
