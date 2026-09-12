using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.SubmitCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationById;
using SmartTaxi.Application.Payments.CashDeclarations.Queries.GetMyCashDeclarations;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Self-service driver cash declarations — DriverId always comes from the caller's own driver profile, never client-supplied.</summary>
public static class CashDeclarationEndpoints
{
    public static IEndpointRouteBuilder MapCashDeclarationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/cash-declarations").WithTags("Cash Declarations").RequireAuthorization(Permissions.FinanceCashDeclarationsSubmitOwn);

        group.MapPost("/", SubmitAsync)
            .WithName("SubmitCashDeclaration").Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/mine", GetMineAsync)
            .WithName("GetMyCashDeclarations").Produces<IReadOnlyCollection<CashDeclarationResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{declarationId:guid}", GetByIdAsync)
            .WithName("GetCashDeclarationById").Produces<CashDeclarationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SubmitAsync(
        SubmitCashDeclarationRequest request, ClaimsPrincipal currentUser, IDriverProfileRepository driverRepository,
        SubmitCashDeclarationCommandHandler handler, CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByUserIdAsync(CurrentUserId(currentUser), cancellationToken);

        if (driver is null)
        {
            return TypedResults.Problem(detail: "Profil chauffeur introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        var result = await handler.Handle(
            new SubmitCashDeclarationCommand(
                driver.Id, request.OwnerId, request.AssignmentId, request.PeriodStart, request.PeriodEnd, request.ExpectedCash, request.DeclaredCash),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<CashDeclarationResponse>>, ProblemHttpResult>> GetMineAsync(
        CashDeclarationStatus? status, int pageNumber, int pageSize, ClaimsPrincipal currentUser, IDriverProfileRepository driverRepository,
        GetMyCashDeclarationsQueryHandler handler, CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByUserIdAsync(CurrentUserId(currentUser), cancellationToken);

        if (driver is null)
        {
            return TypedResults.Problem(detail: "Profil chauffeur introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        var result = await handler.Handle(new GetMyCashDeclarationsQuery(driver.Id, status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<CashDeclarationResponse> response = result.Items.Select(CashDeclarationResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<CashDeclarationResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid declarationId, ClaimsPrincipal currentUser, IDriverProfileRepository driverRepository, GetCashDeclarationByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCashDeclarationByIdQuery(declarationId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var driver = await driverRepository.GetByUserIdAsync(CurrentUserId(currentUser), cancellationToken);

        if (driver is null || driver.Id != result.Value!.DriverId)
        {
            return TypedResults.Problem(detail: "Seul le chauffeur concerné peut consulter cette déclaration.", statusCode: StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(CashDeclarationResponse.FromEntity(result.Value));
    }
}
