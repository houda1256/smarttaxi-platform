using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Owners;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Owners.Commands.CreateCompanyOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Commands.CreateIndividualOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Commands.UpdateOwnerProfile;
using SmartTaxi.Application.Fleet.Owners.Queries.GetMyOwnerProfile;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class OwnerEndpoints
{
    public static IEndpointRouteBuilder MapOwnerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/owner-profile")
            .WithTags("FleetOwners").RequireAuthorization(Permissions.FleetOwnerProfileManageOwn);

        group.MapPost("/individual", CreateIndividualAsync)
            .WithName("CreateIndividualOwnerProfile")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/company", CreateCompanyAsync)
            .WithName("CreateCompanyOwnerProfile")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMineAsync)
            .WithName("GetMyOwnerProfile")
            .Produces<OwnerProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{ownerId:guid}", UpdateAsync)
            .WithName("UpdateOwnerProfile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, ForbidHttpResult, Conflict<ProblemDetails>>> CreateIndividualAsync(
        CreateIndividualOwnerProfileRequest request, ClaimsPrincipal currentUser,
        CreateIndividualOwnerProfileCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateIndividualOwnerProfileCommand(
            userId, request.FirstName, request.LastName, request.NationalId, request.Street, request.City,
            request.PostalCode, request.Country, request.BankName, request.AccountHolderName, request.AccountNumber,
            request.SwiftOrBic);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Forbid(),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, ForbidHttpResult, Conflict<ProblemDetails>>> CreateCompanyAsync(
        CreateCompanyOwnerProfileRequest request, ClaimsPrincipal currentUser,
        CreateCompanyOwnerProfileCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateCompanyOwnerProfileCommand(
            userId, request.LegalName, request.TradeName, request.TaxIdentifier, request.RegistrationNumber,
            request.Street, request.City, request.PostalCode, request.Country, request.BankName,
            request.AccountHolderName, request.AccountNumber, request.SwiftOrBic);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Forbid(),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<OwnerProfileResponse>, NotFound<ProblemDetails>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyOwnerProfileQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetMyOwnerProfileQuery(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Profil introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(OwnerProfileResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ForbidHttpResult, NotFound<ProblemDetails>>> UpdateAsync(
        Guid ownerId, UpdateOwnerProfileRequest request, ClaimsPrincipal currentUser,
        UpdateOwnerProfileCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new UpdateOwnerProfileCommand(
            userId, ownerId, request.FirstName, request.LastName, request.NationalId, request.LegalName,
            request.TradeName, request.TaxIdentifier, request.RegistrationNumber, request.Street, request.City,
            request.PostalCode, request.Country, request.BankName, request.AccountHolderName, request.AccountNumber,
            request.SwiftOrBic);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Forbidden => TypedResults.Forbid(),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.NoContent();
    }
}
