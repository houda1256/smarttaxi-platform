using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Commands.FailPayment;
using SmartTaxi.Application.Payments.Commands.RefundPayment;
using SmartTaxi.Application.Payments.Queries.GetAdminPayments;
using SmartTaxi.Application.Payments.Queries.GetPaymentByIdAdmin;
using SmartTaxi.Application.Payments.Queries.GetPaymentTransactionHistory;
using SmartTaxi.Application.Payments.Queries.GetRefundsForPayment;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

public static class PaymentAdminEndpoints
{
    public static IEndpointRouteBuilder MapPaymentAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var manage = app.MapGroup("/api/admin/payments").WithTags("Payment Administration").RequireAuthorization(Permissions.PaymentsManage);

        manage.MapGet("/", GetAllAsync)
            .WithName("GetAdminPayments").Produces<IReadOnlyCollection<PaymentResponse>>(StatusCodes.Status200OK);

        manage.MapGet("/{paymentId:guid}", GetByIdAsync)
            .WithName("GetPaymentByIdAdmin").Produces<PaymentResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        manage.MapGet("/{paymentId:guid}/history", GetHistoryAsync)
            .WithName("GetPaymentTransactionHistory").Produces<IReadOnlyCollection<PaymentTransactionHistoryResponse>>(StatusCodes.Status200OK);

        manage.MapGet("/{paymentId:guid}/refunds", GetRefundsAsync)
            .WithName("GetRefundsForPayment").Produces<IReadOnlyCollection<RefundRecordResponse>>(StatusCodes.Status200OK);

        manage.MapPost("/{paymentId:guid}/fail", FailAsync)
            .WithName("FailPayment").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var refund = app.MapGroup("/api/admin/payments").WithTags("Payment Administration").RequireAuthorization(Permissions.PaymentsRefund);

        refund.MapPost("/{paymentId:guid}/refund", RefundAsync)
            .WithName("RefundPayment").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<PaymentResponse>>> GetAllAsync(
        Guid? customerId, Guid? driverId, Guid? ownerId, PaymentStatus? status, DateTime? fromDate, DateTime? toDate,
        int pageNumber, int pageSize, GetAdminPaymentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetAdminPaymentsQuery(customerId, driverId, ownerId, status, fromDate, toDate, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<PaymentResponse> response = result.Items.Select(PaymentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<PaymentResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid paymentId, GetPaymentByIdAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPaymentByIdAdminQuery(paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(PaymentResponse.FromSummary(result.Value!)) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<PaymentTransactionHistoryResponse>>> GetHistoryAsync(
        Guid paymentId, GetPaymentTransactionHistoryQueryHandler handler, CancellationToken cancellationToken)
    {
        var history = await handler.Handle(new GetPaymentTransactionHistoryQuery(paymentId), cancellationToken);
        IReadOnlyCollection<PaymentTransactionHistoryResponse> response = history.Select(PaymentTransactionHistoryResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<RefundRecordResponse>>> GetRefundsAsync(
        Guid paymentId, GetRefundsForPaymentQueryHandler handler, CancellationToken cancellationToken)
    {
        var refunds = await handler.Handle(new GetRefundsForPaymentQuery(paymentId), cancellationToken);
        IReadOnlyCollection<RefundRecordResponse> response = refunds.Select(RefundRecordResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> FailAsync(
        Guid paymentId, FailPaymentRequest request, FailPaymentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new FailPaymentCommand(paymentId, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RefundAsync(
        Guid paymentId, RefundPaymentRequest request, ClaimsPrincipal currentUser, RefundPaymentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(
            new RefundPaymentCommand(adminId, paymentId, request.RefundType, request.Amount, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
