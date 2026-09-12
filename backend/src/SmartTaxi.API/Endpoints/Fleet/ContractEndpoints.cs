using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Contracts.Commands.ActivateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SuspendContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.TerminateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.UpdateDraftContract;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetActiveContract;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForDriver;
using SmartTaxi.Application.Fleet.Contracts.Queries.GetContractsForOwner;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class ContractEndpoints
{
    private const string InvalidEnumError = "Valeur d'énumération inconnue.";

    public static IEndpointRouteBuilder MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var owner = app.MapGroup("/api/fleet/contracts")
            .WithTags("Contracts").RequireAuthorization(Permissions.FleetContractsManageOwn);

        owner.MapPost("/", CreateAsync)
            .WithName("CreateContract")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapGet("/owner", GetForOwnerAsync)
            .WithName("GetContractsForOwner")
            .Produces<IReadOnlyCollection<ContractResponse>>(StatusCodes.Status200OK);

        owner.MapPut("/{contractId:guid}", UpdateDraftAsync)
            .WithName("UpdateDraftContract")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        owner.MapPost("/{contractId:guid}/submit", SubmitAsync)
            .WithName("SubmitContract")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{contractId:guid}/activate", ActivateAsync)
            .WithName("ActivateContract")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{contractId:guid}/suspend", SuspendAsync)
            .WithName("SuspendContract")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{contractId:guid}/terminate", TerminateAsync)
            .WithName("TerminateContract")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var driver = app.MapGroup("/api/fleet/contracts")
            .WithTags("Contracts").RequireAuthorization(Permissions.FleetContractsReadOwn);

        driver.MapGet("/driver/{driverId:guid}", GetForDriverAsync)
            .WithName("GetContractsForDriver")
            .Produces<IReadOnlyCollection<ContractResponse>>(StatusCodes.Status200OK);

        driver.MapGet("/active", GetActiveAsync)
            .WithName("GetActiveContract")
            .Produces<ContractResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> CreateAsync(
        CreateContractRequest request, ClaimsPrincipal currentUser, CreateContractCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContractType>(request.ContractType, ignoreCase: true, out var contractType)
            || !Enum.TryParse<PaymentFrequency>(request.PaymentFrequency, ignoreCase: true, out var paymentFrequency))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateContractCommand(
            ownerId, request.DriverId, request.VehicleId, contractType, request.StartDate, request.EndDate,
            request.FixedAmount, request.DriverPercentage, request.OwnerPercentage, paymentFrequency, request.DocumentReference);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.Conflict ? TypedResults.Conflict(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyCollection<ContractResponse>>> GetForOwnerAsync(
        ClaimsPrincipal currentUser, GetContractsForOwnerQueryHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetContractsForOwnerQuery(ownerId), cancellationToken);

        IReadOnlyCollection<ContractResponse> response = summaries.Select(ContractResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> UpdateDraftAsync(
        Guid contractId, UpdateDraftContractRequest request, ClaimsPrincipal currentUser,
        UpdateDraftContractCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ContractType>(request.ContractType, ignoreCase: true, out var contractType)
            || !Enum.TryParse<PaymentFrequency>(request.PaymentFrequency, ignoreCase: true, out var paymentFrequency))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new UpdateDraftContractCommand(
            userId, contractId, contractType, request.StartDate, request.EndDate, request.FixedAmount,
            request.DriverPercentage, request.OwnerPercentage, paymentFrequency, request.DocumentReference);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SubmitAsync(
        Guid contractId, ClaimsPrincipal currentUser, SubmitContractCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitContractCommand(userId, contractId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ActivateAsync(
        Guid contractId, ClaimsPrincipal currentUser, ActivateContractCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ActivateContractCommand(userId, contractId), cancellationToken);

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

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid contractId, ClaimsPrincipal currentUser, SuspendContractCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendContractCommand(userId, contractId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> TerminateAsync(
        Guid contractId, ClaimsPrincipal currentUser, TerminateContractCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new TerminateContractCommand(userId, contractId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Ok<IReadOnlyCollection<ContractResponse>>> GetForDriverAsync(
        Guid driverId, GetContractsForDriverQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetContractsForDriverQuery(driverId), cancellationToken);
        IReadOnlyCollection<ContractResponse> response = summaries.Select(ContractResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ContractResponse>, NotFound<ProblemDetails>>> GetActiveAsync(
        Guid driverId, Guid ownerId, ClaimsPrincipal currentUser, GetActiveContractQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetActiveContractQuery(userId, driverId, ownerId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Contrat introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(ContractResponse.FromSummary(result.Value!));
    }

    private static Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>> ToNoContentResult(Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
        return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.Conflict(problem);
    }
}
