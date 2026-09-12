using SmartTaxi.Application.Payments.Ledger;
using SmartTaxi.Application.Payments.Payouts.Commands.ApprovePayout;
using SmartTaxi.Application.Payments.Payouts.Commands.CancelPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.CompletePayout;
using SmartTaxi.Application.Payments.Payouts.Commands.FailPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.RejectPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.RequestPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.StartProcessingPayout;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Tests.Payments.Payouts;

public class PayoutCommandHandlerTests
{
    private readonly FakeFinancialAccountRepository _accountRepository = new();
    private readonly FakeFinancialLedgerRepository _ledgerRepository;
    private readonly FakePayoutRepository _payoutRepository;
    private readonly FakePayoutPolicy _payoutPolicy = new();

    private readonly RequestPayoutCommandHandler _requestHandler;
    private readonly ApprovePayoutCommandHandler _approveHandler;
    private readonly RejectPayoutCommandHandler _rejectHandler;
    private readonly StartProcessingPayoutCommandHandler _startProcessingHandler;
    private readonly CompletePayoutCommandHandler _completeHandler;
    private readonly FailPayoutCommandHandler _failHandler;
    private readonly CancelPayoutCommandHandler _cancelHandler;

    public PayoutCommandHandlerTests()
    {
        _ledgerRepository = new FakeFinancialLedgerRepository(_accountRepository);
        _payoutRepository = new FakePayoutRepository(_accountRepository, _ledgerRepository);

        _requestHandler = new RequestPayoutCommandHandler(_payoutRepository, _accountRepository, _payoutPolicy);
        _approveHandler = new ApprovePayoutCommandHandler(_payoutRepository);
        _rejectHandler = new RejectPayoutCommandHandler(_payoutRepository);
        _startProcessingHandler = new StartProcessingPayoutCommandHandler(_payoutRepository);
        _completeHandler = new CompletePayoutCommandHandler(_payoutRepository);
        _failHandler = new FailPayoutCommandHandler(_payoutRepository);
        _cancelHandler = new CancelPayoutCommandHandler(_payoutRepository);
    }

    private async Task CreditDriverAvailableBalanceAsync(Guid driverUserId, decimal amount)
    {
        var account = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Driver, driverUserId, "TND", CancellationToken.None);
        _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), amount);
    }

    [Fact]
    public async Task RequestPayout_BelowMinimum_ReturnsValidationError()
    {
        var driverUserId = Guid.NewGuid();
        await CreditDriverAvailableBalanceAsync(driverUserId, 100m);

        var result = await _requestHandler.Handle(
            new RequestPayoutCommand(driverUserId, FinancialAccountType.Driver, driverUserId, 5m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RequestPayout_ExceedingAvailableBalance_ReturnsValidationError()
    {
        var driverUserId = Guid.NewGuid();
        await CreditDriverAvailableBalanceAsync(driverUserId, 50m);

        var result = await _requestHandler.Handle(
            new RequestPayoutCommand(driverUserId, FinancialAccountType.Driver, driverUserId, 500m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RequestPayout_Valid_EntersPendingApprovalImmediately()
    {
        var driverUserId = Guid.NewGuid();
        await CreditDriverAvailableBalanceAsync(driverUserId, 500m);

        var result = await _requestHandler.Handle(
            new RequestPayoutCommand(driverUserId, FinancialAccountType.Driver, driverUserId, 200m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payout = await _payoutRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(PayoutStatus.PendingApproval, payout!.Status);
    }

    private async Task<Guid> CreateProcessingPayoutAsync(Guid driverUserId, decimal amount, decimal creditedBalance)
    {
        await CreditDriverAvailableBalanceAsync(driverUserId, creditedBalance);
        var requestResult = await _requestHandler.Handle(
            new RequestPayoutCommand(driverUserId, FinancialAccountType.Driver, driverUserId, amount, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand),
            CancellationToken.None);
        var payoutId = requestResult.Value;

        var financeManagerId = Guid.NewGuid();
        await _approveHandler.Handle(new ApprovePayoutCommand(financeManagerId, payoutId), CancellationToken.None);
        await _startProcessingHandler.Handle(new StartProcessingPayoutCommand(financeManagerId, payoutId), CancellationToken.None);

        return payoutId;
    }

    [Fact]
    public async Task CompletePayout_MovesAvailableToPaidOutExactlyOnce()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 200m, 500m);

        var result = await _completeHandler.Handle(new CompletePayoutCommand(Guid.NewGuid(), payoutId), CancellationToken.None);
        Assert.True(result.IsSuccess);

        var account = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);
        Assert.Equal(300m, account!.AvailableBalance);
        Assert.Equal(200m, account.PaidOutBalance);

        var payout = await _payoutRepository.GetByIdAsync(payoutId, CancellationToken.None);
        Assert.Equal(PayoutStatus.Paid, payout!.Status);

        // Calling Complete again must be a harmless no-op — balances must not move a second time.
        var second = await _completeHandler.Handle(new CompletePayoutCommand(Guid.NewGuid(), payoutId), CancellationToken.None);
        Assert.True(second.IsSuccess);
        var accountAfterSecondCall = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);
        Assert.Equal(300m, accountAfterSecondCall!.AvailableBalance);
        Assert.Equal(200m, accountAfterSecondCall.PaidOutBalance);
    }

    [Fact]
    public async Task CompletePayout_PostsSingleLedgerEntry()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 150m, 500m);

        await _completeHandler.Handle(new CompletePayoutCommand(Guid.NewGuid(), payoutId), CancellationToken.None);

        var entries = await _ledgerRepository.GetForSourceAsync("Payout", payoutId, CancellationToken.None);
        Assert.Single(entries);
        Assert.Equal(Domain.Payments.Ledger.Enums.LedgerEntryType.Payout, entries.Single().EntryType);
    }

    [Fact]
    public async Task CompletePayout_WhenBalanceDropsBelowAmount_ReturnsConflict()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 400m, 500m);

        // Simulate the balance being consumed by something else while the payout was in flight.
        var account = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);
        _accountRepository.SetProperty(account!.Id, nameof(account.AvailableBalance), 100m);

        var result = await _completeHandler.Handle(new CompletePayoutCommand(Guid.NewGuid(), payoutId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Conflict, result.ErrorType);
        var payout = await _payoutRepository.GetByIdAsync(payoutId, CancellationToken.None);
        Assert.Equal(PayoutStatus.Processing, payout!.Status);
    }

    [Fact]
    public async Task RejectPayout_FromPendingApproval_Succeeds()
    {
        var driverUserId = Guid.NewGuid();
        await CreditDriverAvailableBalanceAsync(driverUserId, 500m);
        var requestResult = await _requestHandler.Handle(
            new RequestPayoutCommand(driverUserId, FinancialAccountType.Driver, driverUserId, 100m, "TND", PayoutMethod.BankTransfer, PayoutFrequency.OnDemand),
            CancellationToken.None);

        var result = await _rejectHandler.Handle(new RejectPayoutCommand(Guid.NewGuid(), requestResult.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payout = await _payoutRepository.GetByIdAsync(requestResult.Value, CancellationToken.None);
        Assert.Equal(PayoutStatus.Rejected, payout!.Status);
    }

    [Fact]
    public async Task CancelPayout_AfterProcessingStarted_Fails()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 100m, 500m);

        var result = await _cancelHandler.Handle(new CancelPayoutCommand(Guid.NewGuid(), payoutId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FailPayout_WithoutReason_ReturnsValidationError()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 100m, 500m);

        var result = await _failHandler.Handle(new FailPayoutCommand(Guid.NewGuid(), payoutId, ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Common.ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task FailPayout_WhileProcessing_Succeeds()
    {
        var driverUserId = Guid.NewGuid();
        var payoutId = await CreateProcessingPayoutAsync(driverUserId, 100m, 500m);

        var result = await _failHandler.Handle(new FailPayoutCommand(Guid.NewGuid(), payoutId, "Coordonnées bancaires invalides"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payout = await _payoutRepository.GetByIdAsync(payoutId, CancellationToken.None);
        Assert.Equal(PayoutStatus.Failed, payout!.Status);
    }
}
