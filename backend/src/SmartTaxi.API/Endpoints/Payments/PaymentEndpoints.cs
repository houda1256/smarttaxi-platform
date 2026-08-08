using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Commands.AuthorizePayment;
using SmartTaxi.Application.Payments.Commands.CancelPayment;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Payments.Queries.GetInvoiceByPaymentId;
using SmartTaxi.Application.Payments.Queries.GetMyPaymentsForCustomer;
using SmartTaxi.Application.Payments.Queries.GetMyPaymentsForDriver;
using SmartTaxi.Application.Payments.Queries.GetPaymentById;
using SmartTaxi.Application.Payments.Queries.GetPaymentsForOwner;
using SmartTaxi.Application.Payments.Queries.GetReceiptByPaymentId;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Route convention matches Ride/Fleet: flat /api/payments, no /api/v1 prefix — see RideEndpoints' doc comment for the reasoning.</summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapPost("/", CreateAsync).RequireAuthorization(Permissions.PaymentsCreate)
            .WithName("CreateRidePayment").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{paymentId:guid}/authorize", AuthorizeAsync).RequireAuthorization(Permissions.PaymentsConfirm)
            .WithName("AuthorizePayment").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{paymentId:guid}/confirm", ConfirmAsync).RequireAuthorization(Permissions.PaymentsConfirm)
            .WithName("ConfirmPayment").Produces<decimal>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{paymentId:guid}/cancel", CancelAsync).RequireAuthorization(Permissions.PaymentsCancel)
            .WithName("CancelPayment").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/mine", GetMineForCustomerAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetMyPaymentsAsCustomer").Produces<IReadOnlyCollection<PaymentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/driver/mine", GetMineForDriverAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetMyPaymentsAsDriver").Produces<IReadOnlyCollection<PaymentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/owner/mine", GetMineForOwnerAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetMyPaymentsAsOwner").Produces<IReadOnlyCollection<PaymentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{paymentId:guid}", GetByIdAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetPaymentById").Produces<PaymentResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{paymentId:guid}/invoice", GetInvoiceAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetPaymentInvoice").Produces<InvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{paymentId:guid}/receipt", GetReceiptAsync).RequireAuthorization(Permissions.PaymentsReadOwn)
            .WithName("GetPaymentReceipt").Produces<ReceiptResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateAsync(
        CreateRidePaymentRequest request, ClaimsPrincipal currentUser, CreateRidePaymentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateRidePaymentCommand(CurrentUserId(currentUser), request.RideId, request.PaymentMethod), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AuthorizeAsync(
        Guid paymentId, ClaimsPrincipal currentUser, AuthorizePaymentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AuthorizePaymentCommand(CurrentUserId(currentUser), paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<decimal>, ProblemHttpResult>> ConfirmAsync(
        Guid paymentId, ClaimsPrincipal currentUser, ConfirmPaymentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ConfirmPaymentCommand(CurrentUserId(currentUser), paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid paymentId, CancelPaymentRequest request, ClaimsPrincipal currentUser, CancelPaymentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelPaymentCommand(CurrentUserId(currentUser), paymentId, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<PaymentResponse>>> GetMineForCustomerAsync(
        ClaimsPrincipal currentUser, GetMyPaymentsForCustomerQueryHandler handler, CancellationToken cancellationToken)
    {
        var payments = await handler.Handle(new GetMyPaymentsForCustomerQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<PaymentResponse> response = payments.Select(PaymentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IReadOnlyCollection<PaymentResponse>>, ProblemHttpResult>> GetMineForDriverAsync(
        ClaimsPrincipal currentUser, IDriverProfileRepository driverRepository, GetMyPaymentsForDriverQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByUserIdAsync(CurrentUserId(currentUser), cancellationToken);

        if (driver is null)
        {
            return TypedResults.Problem(detail: "Profil chauffeur introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        var payments = await handler.Handle(new GetMyPaymentsForDriverQuery(driver.Id), cancellationToken);
        IReadOnlyCollection<PaymentResponse> response = payments.Select(PaymentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<PaymentResponse>>> GetMineForOwnerAsync(
        ClaimsPrincipal currentUser, GetPaymentsForOwnerQueryHandler handler, CancellationToken cancellationToken)
    {
        var payments = await handler.Handle(new GetPaymentsForOwnerQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<PaymentResponse> response = payments.Select(PaymentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<PaymentResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid paymentId, ClaimsPrincipal currentUser, GetPaymentByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPaymentByIdQuery(CurrentUserId(currentUser), paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(PaymentResponse.FromSummary(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> GetInvoiceAsync(
        Guid paymentId, ClaimsPrincipal currentUser, IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository,
        GetInvoiceByPaymentIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifyPaymentParticipantAsync(paymentId, CurrentUserId(currentUser), paymentRepository, driverRepository, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await handler.Handle(new GetInvoiceByPaymentIdQuery(paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(InvoiceResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<ReceiptResponse>, ProblemHttpResult>> GetReceiptAsync(
        Guid paymentId, ClaimsPrincipal currentUser, IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository,
        GetReceiptByPaymentIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifyPaymentParticipantAsync(paymentId, CurrentUserId(currentUser), paymentRepository, driverRepository, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await handler.Handle(new GetReceiptByPaymentIdQuery(paymentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(ReceiptResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    /// <summary>GetInvoiceByPaymentId/GetReceiptByPaymentId look up documents by PaymentId alone with no ownership check of their own, so the endpoint enforces "only a participant may view" itself before calling them.</summary>
    private static async Task<ProblemHttpResult?> VerifyPaymentParticipantAsync(
        Guid paymentId, Guid requestingUserId, IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository,
        CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return TypedResults.Problem(detail: "Paiement introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        if (requestingUserId == payment.CustomerId || requestingUserId == payment.OwnerId)
        {
            return null;
        }

        var driver = await driverRepository.GetByIdAsync(payment.DriverId, cancellationToken);

        if (driver is not null && driver.UserId == requestingUserId)
        {
            return null;
        }

        return TypedResults.Problem(detail: "Seuls les participants au paiement peuvent le consulter.", statusCode: StatusCodes.Status403Forbidden);
    }
}
