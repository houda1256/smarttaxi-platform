using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Application.Payments.Disputes.Commands.EscalateFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.OpenFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.RejectFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.ResolveFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.StartReviewFinancialDispute;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Tests.Payments.Disputes;

public class FinancialDisputeCommandHandlerTests
{
    private readonly FakeFinancialAccountRepository _accountRepository = new();
    private readonly FakeFinancialLedgerRepository _ledgerRepository;
    private readonly FakePayoutRepository _payoutRepository;
    private readonly FakeFinancialDisputeRepository _disputeRepository;

    private readonly OpenFinancialDisputeCommandHandler _openHandler;
    private readonly StartReviewFinancialDisputeCommandHandler _startReviewHandler;
    private readonly ResolveFinancialDisputeCommandHandler _resolveHandler;
    private readonly RejectFinancialDisputeCommandHandler _rejectHandler;
    private readonly EscalateFinancialDisputeCommandHandler _escalateHandler;

    public FinancialDisputeCommandHandlerTests()
    {
        _ledgerRepository = new FakeFinancialLedgerRepository(_accountRepository);
        _payoutRepository = new FakePayoutRepository(_accountRepository, _ledgerRepository);
        _disputeRepository = new FakeFinancialDisputeRepository(_accountRepository, _payoutRepository);

        _openHandler = new OpenFinancialDisputeCommandHandler(_disputeRepository);
        _startReviewHandler = new StartReviewFinancialDisputeCommandHandler(_disputeRepository);
        _resolveHandler = new ResolveFinancialDisputeCommandHandler(_disputeRepository);
        _rejectHandler = new RejectFinancialDisputeCommandHandler(_disputeRepository);
        _escalateHandler = new EscalateFinancialDisputeCommandHandler(_disputeRepository);
    }

    private async Task CreditPlatformAvailableBalanceAsync(decimal amount)
    {
        var account = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, "TND", CancellationToken.None);
        _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), amount);
    }

    [Fact]
    public async Task OpenDispute_MovesDisputedAmountFromAvailableToReserved()
    {
        await CreditPlatformAvailableBalanceAsync(1000m);
        var paymentId = Guid.NewGuid();

        var result = await _openHandler.Handle(
            new OpenFinancialDisputeCommand(
                Guid.NewGuid(), FinancialDisputeCategory.IncorrectFare, paymentId, null, null, 150m, "TND", "Tarif incorrect facturé", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var platform = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        Assert.Equal(850m, platform!.AvailableBalance);
        Assert.Equal(150m, platform.ReservedBalance);
    }

    [Fact]
    public async Task OpenDispute_ExceedingAvailableBalance_Fails()
    {
        await CreditPlatformAvailableBalanceAsync(100m);

        var result = await _openHandler.Handle(
            new OpenFinancialDisputeCommand(
                Guid.NewGuid(), FinancialDisputeCategory.DuplicatePayment, Guid.NewGuid(), null, null, 500m, "TND", "Paiement en double", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task OpenDispute_WithoutAnyRelatedReference_ReturnsValidationError()
    {
        await CreditPlatformAvailableBalanceAsync(1000m);

        var result = await _openHandler.Handle(
            new OpenFinancialDisputeCommand(Guid.NewGuid(), FinancialDisputeCategory.Other, null, null, null, 50m, "TND", "Litige sans référence", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Validation, result.ErrorType);
    }

    private async Task<Guid> OpenAndStartReviewDisputeAsync(decimal amount)
    {
        await CreditPlatformAvailableBalanceAsync(1000m);
        var openResult = await _openHandler.Handle(
            new OpenFinancialDisputeCommand(
                Guid.NewGuid(), FinancialDisputeCategory.RefundIssue, Guid.NewGuid(), null, null, amount, "TND", "Remboursement non reçu", null),
            CancellationToken.None);

        var financeManagerId = Guid.NewGuid();
        await _startReviewHandler.Handle(new StartReviewFinancialDisputeCommand(financeManagerId, openResult.Value), CancellationToken.None);

        return openResult.Value;
    }

    [Fact]
    public async Task ResolveWithRelease_ReturnsReservedAmountToAvailable()
    {
        var disputeId = await OpenAndStartReviewDisputeAsync(200m);

        var result = await _resolveHandler.Handle(
            new ResolveFinancialDisputeCommand(Guid.NewGuid(), disputeId, "Remboursement confirmé effectué", DisputeResolutionOutcome.Release),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var platform = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        Assert.Equal(1000m, platform!.AvailableBalance);
        Assert.Equal(0m, platform.ReservedBalance);
    }

    [Fact]
    public async Task ResolveWithAdjust_PermanentlyRemovesAmountFromReserved_WithoutReturningToAvailable()
    {
        var disputeId = await OpenAndStartReviewDisputeAsync(200m);

        var result = await _resolveHandler.Handle(
            new ResolveFinancialDisputeCommand(Guid.NewGuid(), disputeId, "Montant définitivement ajusté", DisputeResolutionOutcome.Adjust),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var platform = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        Assert.Equal(800m, platform!.AvailableBalance);
        Assert.Equal(0m, platform.ReservedBalance);
    }

    [Fact]
    public async Task Reject_AlwaysReleasesTheReservation()
    {
        var disputeId = await OpenAndStartReviewDisputeAsync(200m);

        var result = await _rejectHandler.Handle(new RejectFinancialDisputeCommand(Guid.NewGuid(), disputeId, "Litige non fondé"), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var platform = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        Assert.Equal(1000m, platform!.AvailableBalance);
        Assert.Equal(0m, platform.ReservedBalance);
    }

    [Fact]
    public async Task Escalate_KeepsFundsReserved()
    {
        var disputeId = await OpenAndStartReviewDisputeAsync(200m);

        var result = await _escalateHandler.Handle(new EscalateFinancialDisputeCommand(Guid.NewGuid(), disputeId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var platform = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        Assert.Equal(800m, platform!.AvailableBalance);
        Assert.Equal(200m, platform.ReservedBalance);

        var dispute = await _disputeRepository.GetByIdAsync(disputeId, CancellationToken.None);
        Assert.Equal(FinancialDisputeStatus.Escalated, dispute!.Status);
    }

    [Fact]
    public async Task Resolve_WithoutStartingReviewFirst_Fails()
    {
        await CreditPlatformAvailableBalanceAsync(1000m);
        var openResult = await _openHandler.Handle(
            new OpenFinancialDisputeCommand(
                Guid.NewGuid(), FinancialDisputeCategory.WrongCommission, Guid.NewGuid(), null, null, 100m, "TND", "Commission incorrecte", null),
            CancellationToken.None);

        var result = await _resolveHandler.Handle(
            new ResolveFinancialDisputeCommand(Guid.NewGuid(), openResult.Value, "Résolution", DisputeResolutionOutcome.Release), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Conflict, result.ErrorType);
    }
}
