using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.LoginUser;
using SmartTaxi.Application.Identity.Commands.RegisterUser;

namespace SmartTaxi.API.Endpoints.Identity;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .WithName("LoginUser")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<Results<Created<RegisterUserResponse>, BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> RegisterAsync(
        RegisterUserRequest request,
        RegisterUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Password);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var isConflict = result.ErrorType == ErrorType.Conflict;

            var problem = new ProblemDetails
            {
                Title = isConflict ? "Conflit" : "Requête invalide",
                Detail = result.Error,
                Status = isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest
            };

            return isConflict
                ? TypedResults.Conflict(problem)
                : TypedResults.BadRequest(problem);
        }

        var response = new RegisterUserResponse(result.Value!.UserId, result.Value.Email);
        return TypedResults.Created($"/api/users/{response.UserId}", response);
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        LoginUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                title: "Non autorisé",
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new LoginResponse(result.Value!.AccessToken));
    }
}
