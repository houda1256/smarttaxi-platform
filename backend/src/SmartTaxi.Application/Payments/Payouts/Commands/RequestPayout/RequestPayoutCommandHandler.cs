using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Application.Payments.Payouts.Commands.RequestPayout;

/// <summary>
/// AvailableBalance is only pre-checked here for a fast, friendly rejection —
/// the authoritative, race-proof check happens again atomically when the
/// Payout actually completes (IPayoutRepository.TryCompleteAsync), since the
/// balance can change while a Payout sits in Requested/PendingApproval/Approved.
/// </summary>
public sealed class RequestPayoutCommandHandler : ICommandHandler<RequestPayoutCommand, Result<Guid>>
{
    private const string BelowMinimumError = "Le montant du versement est inférieur au minimum autorisé.";
    private const string InsufficientBalanceError = "Le solde disponible est insuffisant pour ce versement.";

    private readonly IPayoutRepository _payoutRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IPayoutPolicy _payoutPolicy;

    public RequestPayoutCommandHandler(
        IPayoutRepository payoutRepository, IFinancialAccountRepository accountRepository, IPayoutPolicy payoutPolicy)
    {
        _payoutRepository = payoutRepository;
        _accountRepository = accountRepository;
        _payoutPolicy = payoutPolicy;
    }

    public async Task<Result<Guid>> Handle(RequestPayoutCommand command, CancellationToken cancellationToken)
    {
        if (command.Amount < _payoutPolicy.MinimumPayoutAmount)
        {
            return Result<Guid>.Failure(BelowMinimumError, ErrorType.Validation);
        }

        var account = await _accountRepository.GetOrCreateAsync(
            command.BeneficiaryType, command.BeneficiaryOwnerReferenceId, command.Currency, cancellationToken);

        if (account.AvailableBalance < command.Amount)
        {
            return Result<Guid>.Failure(InsufficientBalanceError, ErrorType.Validation);
        }

        Payout payout;

        try
        {
            payout = Payout.Request(account.Id, command.BeneficiaryType, command.Amount, command.Currency, command.Method, command.Frequency, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _payoutRepository.AddAsync(payout, cancellationToken);

        // Requested is a momentary, audit-only status — every request immediately
        // enters the finance queue as PendingApproval, so there is no separate
        // human-facing "submit" step between requesting and awaiting review.
        await _payoutRepository.TrySubmitForApprovalAsync(payout.Id, DateTime.UtcNow, cancellationToken);

        return Result<Guid>.Success(payout.Id);
    }
}
