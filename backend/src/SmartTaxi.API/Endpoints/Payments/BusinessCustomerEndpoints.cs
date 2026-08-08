using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerEmployees;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetMyBusinessCustomer;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Self-service read access for a BusinessCustomer's own authorized employees — resolved through the caller's active employee membership, never a client-supplied BusinessCustomerId.</summary>
public static class BusinessCustomerEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/business-customers").WithTags("Business Customers").RequireAuthorization(Permissions.FinanceBusinessCustomersReadOwn);

        group.MapGet("/mine", GetMineAsync)
            .WithName("GetMyBusinessCustomer").Produces<BusinessCustomerResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/mine/employees", GetMyEmployeesAsync)
            .WithName("GetMyBusinessCustomerEmployees").Produces<IReadOnlyCollection<BusinessCustomerEmployeeResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<BusinessCustomerResponse>, ProblemHttpResult>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyBusinessCustomerQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMyBusinessCustomerQuery(CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(BusinessCustomerResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<BusinessCustomerEmployeeResponse>>, ProblemHttpResult>> GetMyEmployeesAsync(
        ClaimsPrincipal currentUser, GetMyBusinessCustomerQueryHandler customerHandler, GetBusinessCustomerEmployeesQueryHandler employeesHandler,
        CancellationToken cancellationToken)
    {
        var customerResult = await customerHandler.Handle(new GetMyBusinessCustomerQuery(CurrentUserId(currentUser)), cancellationToken);

        if (!customerResult.IsSuccess)
        {
            return customerResult.ToProblem();
        }

        var employees = await employeesHandler.Handle(new GetBusinessCustomerEmployeesQuery(customerResult.Value!.Id), cancellationToken);
        IReadOnlyCollection<BusinessCustomerEmployeeResponse> response = employees.Select(BusinessCustomerEmployeeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }
}
