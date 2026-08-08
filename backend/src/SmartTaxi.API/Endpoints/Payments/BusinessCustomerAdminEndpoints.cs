using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.AddBusinessCustomerEmployee;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.CloseBusinessCustomer;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.DeactivateBusinessCustomerEmployee;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.ReactivateBusinessCustomer;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.RegisterBusinessCustomer;
using SmartTaxi.Application.Payments.BusinessCustomers.Commands.SuspendBusinessCustomer;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetAllBusinessCustomers;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerById;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetBusinessCustomerEmployees;
using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

public static class BusinessCustomerAdminEndpoints
{
    public static IEndpointRouteBuilder MapBusinessCustomerAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/business-customers").WithTags("Business Customer Administration").RequireAuthorization(Permissions.FinanceBusinessCustomersManage);

        group.MapPost("/", RegisterAsync)
            .WithName("RegisterBusinessCustomer").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetAllAsync)
            .WithName("GetAllBusinessCustomers").Produces<IReadOnlyCollection<BusinessCustomerResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{businessCustomerId:guid}", GetByIdAsync)
            .WithName("GetBusinessCustomerByIdAdmin").Produces<BusinessCustomerResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{businessCustomerId:guid}/suspend", SuspendAsync)
            .WithName("SuspendBusinessCustomer").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{businessCustomerId:guid}/reactivate", ReactivateAsync)
            .WithName("ReactivateBusinessCustomer").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{businessCustomerId:guid}/close", CloseAsync)
            .WithName("CloseBusinessCustomer").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{businessCustomerId:guid}/employees", GetEmployeesAsync)
            .WithName("GetBusinessCustomerEmployeesAdmin").Produces<IReadOnlyCollection<BusinessCustomerEmployeeResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{businessCustomerId:guid}/employees", AddEmployeeAsync)
            .WithName("AddBusinessCustomerEmployee").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/employees/{employeeId:guid}/deactivate", DeactivateEmployeeAsync)
            .WithName("DeactivateBusinessCustomerEmployee").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RegisterAsync(
        RegisterBusinessCustomerRequest request, RegisterBusinessCustomerCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new RegisterBusinessCustomerCommand(
                request.LegalName, request.TaxIdentifier, request.BillingAddress, request.ContactPersonName, request.ContactPersonEmail,
                request.ContactPersonPhone, request.PaymentTerms, request.CreditLimit),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<BusinessCustomerResponse>>> GetAllAsync(
        BusinessCustomerStatus? status, int pageNumber, int pageSize, GetAllBusinessCustomersQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetAllBusinessCustomersQuery(status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<BusinessCustomerResponse> response = result.Items.Select(BusinessCustomerResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<BusinessCustomerResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid businessCustomerId, GetBusinessCustomerByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetBusinessCustomerByIdQuery(businessCustomerId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(BusinessCustomerResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SuspendAsync(
        Guid businessCustomerId, SuspendBusinessCustomerCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SuspendBusinessCustomerCommand(businessCustomerId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReactivateAsync(
        Guid businessCustomerId, ReactivateBusinessCustomerCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReactivateBusinessCustomerCommand(businessCustomerId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CloseAsync(
        Guid businessCustomerId, CloseBusinessCustomerCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CloseBusinessCustomerCommand(businessCustomerId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<BusinessCustomerEmployeeResponse>>> GetEmployeesAsync(
        Guid businessCustomerId, GetBusinessCustomerEmployeesQueryHandler handler, CancellationToken cancellationToken)
    {
        var employees = await handler.Handle(new GetBusinessCustomerEmployeesQuery(businessCustomerId), cancellationToken);
        IReadOnlyCollection<BusinessCustomerEmployeeResponse> response = employees.Select(BusinessCustomerEmployeeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> AddEmployeeAsync(
        Guid businessCustomerId, AddBusinessCustomerEmployeeRequest request, AddBusinessCustomerEmployeeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AddBusinessCustomerEmployeeCommand(
                businessCustomerId, request.UserId, request.Role, request.RideBudgetPerMonth, request.AllowedVehicleCategories,
                request.AllowedScheduleStart, request.AllowedScheduleEnd, request.AllowedZones, request.PerRideLimit),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateEmployeeAsync(
        Guid employeeId, DeactivateBusinessCustomerEmployeeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeactivateBusinessCustomerEmployeeCommand(employeeId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
