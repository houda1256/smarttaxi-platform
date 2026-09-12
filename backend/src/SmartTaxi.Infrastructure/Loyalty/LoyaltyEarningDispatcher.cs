using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Commands.ProcessPaymentLoyaltyAward;
using SmartTaxi.Application.Loyalty.Contracts;

namespace SmartTaxi.Infrastructure.Loyalty;

/// <summary>
/// The concrete implementation behind ILoyaltyEarningDispatcher — lives in
/// Infrastructure only because it needs ILogger (Application takes no NuGet
/// package references at all — see NotificationDispatcher's own doc comment
/// for the identical reasoning). All the real earning/referral/challenge logic
/// lives in the fully unit-testable ProcessPaymentLoyaltyAwardCommandHandler;
/// this class is only a thin try/catch shim so a Loyalty failure can never
/// fail the Payment confirmation that triggered it.
/// </summary>
internal sealed class LoyaltyEarningDispatcher : ILoyaltyEarningDispatcher
{
    private readonly ProcessPaymentLoyaltyAwardCommandHandler _handler;
    private readonly ILogger<LoyaltyEarningDispatcher> _logger;

    public LoyaltyEarningDispatcher(ProcessPaymentLoyaltyAwardCommandHandler handler, ILogger<LoyaltyEarningDispatcher> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task AwardForPaymentAsync(LoyaltyPaymentAwardRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _handler.Handle(
                new ProcessPaymentLoyaltyAwardCommand(request.PayerUserId, request.PayerRole, request.PaymentId, request.Amount),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loyalty earning award failed for payment {PaymentId}.", request.PaymentId);
        }
    }
}
