using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.AdjustPoints;

public sealed class AdjustPointsCommandHandler : ICommandHandler<AdjustPointsCommand, Result>
{
    private const string AdjustmentSourceType = "AdminAdjustment";
    private const string NoAccountError = "Aucun compte fidélité trouvé pour cet utilisateur.";
    private const string ZeroAmountError = "Le montant de l'ajustement doit être différent de zéro.";
    private const string InsufficientBalanceError = "Le solde est insuffisant pour cet ajustement négatif.";

    private readonly ILoyaltyAccountRepository _accountRepository;
    private readonly ILoyaltyPointLedgerRepository _ledgerRepository;
    private readonly ILoyaltyTierThresholdRepository _tierThresholdRepository;

    public AdjustPointsCommandHandler(
        ILoyaltyAccountRepository accountRepository, ILoyaltyPointLedgerRepository ledgerRepository,
        ILoyaltyTierThresholdRepository tierThresholdRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
        _tierThresholdRepository = tierThresholdRepository;
    }

    public async Task<Result> Handle(AdjustPointsCommand command, CancellationToken cancellationToken)
    {
        if (command.Points == 0)
        {
            return Result.Failure(ZeroAmountError, ErrorType.Validation);
        }

        var account = await _accountRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(NoAccountError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;

        if (command.Points > 0)
        {
            var thresholds = await _tierThresholdRepository.GetAllAsync(cancellationToken);

            await _ledgerRepository.TryCreditAsync(
                new LoyaltyLedgerAppendRequest(
                    account.Id, account.UserId, command.PointType, LoyaltyLedgerEntryType.AdminAdjustment, command.Points,
                    AdjustmentSourceType, Guid.NewGuid(), command.Reason, CreatedBy: command.AdminUserId),
                thresholds, utcNow, cancellationToken);

            return Result.Success();
        }

        if (command.PointType != LoyaltyPointType.RewardPoints)
        {
            // StatusPoints have no configured debit path today (no spend/expire concept for them) — see the audit's tier-reset follow-up note.
            return Result.Failure(InsufficientBalanceError, ErrorType.Validation);
        }

        var debited = await _ledgerRepository.TryDebitAsync(
            new LoyaltyLedgerAppendRequest(
                account.Id, account.UserId, command.PointType, LoyaltyLedgerEntryType.AdminAdjustment, -command.Points, AdjustmentSourceType,
                Guid.NewGuid(), command.Reason, CreatedBy: command.AdminUserId),
            utcNow, cancellationToken);

        return debited is not null ? Result.Success() : Result.Failure(InsufficientBalanceError, ErrorType.Conflict);
    }
}
