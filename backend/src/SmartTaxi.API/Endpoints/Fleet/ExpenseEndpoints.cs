using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Expenses;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Expenses.Commands.ApproveExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.MarkExpensePaid;
using SmartTaxi.Application.Fleet.Expenses.Commands.RejectExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.SubmitExpense;
using SmartTaxi.Application.Fleet.Expenses.Queries.ListExpenses;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class ExpenseEndpoints
{
    private const string InvalidEnumError = "Valeur d'énumération inconnue.";

    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/expenses")
            .WithTags("Expenses").RequireAuthorization(Permissions.FleetExpensesManageOwn);

        group.MapPost("/", CreateAsync)
            .WithName("CreateExpense")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .WithName("ListExpenses")
            .Produces<PagedExpenseResponse>(StatusCodes.Status200OK);

        group.MapPost("/{expenseId:guid}/submit", SubmitAsync)
            .WithName("SubmitExpense")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{expenseId:guid}/approve", ApproveAsync)
            .WithName("ApproveExpense")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{expenseId:guid}/reject", RejectAsync)
            .WithName("RejectExpense")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{expenseId:guid}/mark-paid", MarkPaidAsync)
            .WithName("MarkExpensePaid")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> CreateAsync(
        CreateExpenseRequest request, ClaimsPrincipal currentUser, CreateExpenseCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExpenseCategory>(request.Category, ignoreCase: true, out var category))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateExpenseCommand(
            ownerId, request.FleetId, request.VehicleId, request.DriverId, category, request.Amount, request.Currency,
            request.ExpenseDate, request.Description, request.ReceiptReference);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<PagedExpenseResponse>, BadRequest<ProblemDetails>>> ListAsync(
        ClaimsPrincipal currentUser, ListExpensesQueryHandler handler, CancellationToken cancellationToken,
        Guid? fleetId = null, Guid? vehicleId = null, Guid? driverId = null, string? category = null, string? status = null,
        DateOnly? fromDate = null, DateOnly? toDate = null, int pageNumber = 1, int pageSize = 20)
    {
        ExpenseCategory? parsedCategory = null;
        ExpenseStatus? parsedStatus = null;

        if (category is not null)
        {
            if (!Enum.TryParse<ExpenseCategory>(category, ignoreCase: true, out var categoryValue))
            {
                return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
            }

            parsedCategory = categoryValue;
        }

        if (status is not null)
        {
            if (!Enum.TryParse<ExpenseStatus>(status, ignoreCase: true, out var statusValue))
            {
                return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
            }

            parsedStatus = statusValue;
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var query = new ListExpensesQuery(
            ownerId, fleetId, vehicleId, driverId, parsedCategory, parsedStatus, fromDate, toDate, pageNumber, pageSize);

        var result = await handler.Handle(query, cancellationToken);

        var response = new PagedExpenseResponse(
            result.Items.Select(ExpenseResponse.FromSummary).ToList(), result.TotalCount, result.PageNumber, result.PageSize);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SubmitAsync(
        Guid expenseId, ClaimsPrincipal currentUser, SubmitExpenseCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitExpenseCommand(userId, expenseId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid expenseId, ClaimsPrincipal currentUser, ApproveExpenseCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveExpenseCommand(userId, expenseId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectAsync(
        Guid expenseId, ClaimsPrincipal currentUser, RejectExpenseCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RejectExpenseCommand(userId, expenseId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> MarkPaidAsync(
        Guid expenseId, ClaimsPrincipal currentUser, MarkExpensePaidCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new MarkExpensePaidCommand(userId, expenseId), cancellationToken);
        return ToNoContentResult(result);
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
