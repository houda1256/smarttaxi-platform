using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.ApproveCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.DisputeCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.SettleCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.StartReviewCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.SubmitCashDeclaration;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Application.Tests.Payments.CashDeclarations;

public class CashDeclarationCommandHandlerTests
{
    private readonly FakeCashDeclarationRepository _declarationRepository = new();
    private readonly FakeDriverOwnerContractRepository _contractRepository = new();

    private readonly SubmitCashDeclarationCommandHandler _submitHandler;
    private readonly StartReviewCashDeclarationCommandHandler _startReviewHandler;
    private readonly ApproveCashDeclarationCommandHandler _approveHandler;
    private readonly DisputeCashDeclarationCommandHandler _disputeHandler;
    private readonly SettleCashDeclarationCommandHandler _settleHandler;

    public CashDeclarationCommandHandlerTests()
    {
        _submitHandler = new SubmitCashDeclarationCommandHandler(_declarationRepository, _contractRepository);
        _startReviewHandler = new StartReviewCashDeclarationCommandHandler(_declarationRepository);
        _approveHandler = new ApproveCashDeclarationCommandHandler(_declarationRepository);
        _disputeHandler = new DisputeCashDeclarationCommandHandler(_declarationRepository);
        _settleHandler = new SettleCashDeclarationCommandHandler(_declarationRepository);
    }

    [Fact]
    public async Task Submit_WithNoActiveContract_UsesDriverKeepsCashModel()
    {
        var driverId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var result = await _submitHandler.Handle(
            new SubmitCashDeclarationCommand(driverId, ownerId, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 200m, 200m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var declaration = await _declarationRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(CashDeclarationOperatingModel.DriverKeepsCashOwesShare, declaration!.OperatingModel);
    }

    [Fact]
    public async Task Submit_WithPercentagePerRideContract_UsesDriverKeepsCashModel()
    {
        var driverId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var contract = DriverOwnerContract.CreateDraft(
            ownerId, driverId, null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 70m, 30m, PaymentFrequency.Weekly, null, DateTime.UtcNow);
        await _contractRepository.AddAsync(contract, CancellationToken.None);
        await _contractRepository.TrySubmitAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);
        await _contractRepository.TryActivateAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _submitHandler.Handle(
            new SubmitCashDeclarationCommand(driverId, ownerId, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 200m, 180m),
            CancellationToken.None);

        var declaration = await _declarationRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(CashDeclarationOperatingModel.DriverKeepsCashOwesShare, declaration!.OperatingModel);
        Assert.Equal(-20m, declaration.Difference);
    }

    [Fact]
    public async Task Submit_WithFixedSalaryContract_UsesDriverRemitsCashModel()
    {
        var driverId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var contract = DriverOwnerContract.CreateDraft(
            ownerId, driverId, null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            1500m, null, null, PaymentFrequency.Monthly, null, DateTime.UtcNow);
        await _contractRepository.AddAsync(contract, CancellationToken.None);
        await _contractRepository.TrySubmitAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);
        await _contractRepository.TryActivateAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _submitHandler.Handle(
            new SubmitCashDeclarationCommand(driverId, ownerId, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 300m, 300m),
            CancellationToken.None);

        var declaration = await _declarationRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(CashDeclarationOperatingModel.DriverRemitsCashToOwner, declaration!.OperatingModel);
    }

    private async Task<Guid> SubmitDeclarationAsync()
    {
        var result = await _submitHandler.Handle(
            new SubmitCashDeclarationCommand(Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), 100m, 100m),
            CancellationToken.None);
        return result.Value;
    }

    [Fact]
    public async Task ApproveDeclaration_DirectlyFromSubmitted_Succeeds()
    {
        var declarationId = await SubmitDeclarationAsync();

        var result = await _approveHandler.Handle(new ApproveCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var declaration = await _declarationRepository.GetByIdAsync(declarationId, CancellationToken.None);
        Assert.Equal(CashDeclarationStatus.Approved, declaration!.Status);
    }

    [Fact]
    public async Task StartReview_ThenDispute_Succeeds()
    {
        var declarationId = await SubmitDeclarationAsync();
        await _startReviewHandler.Handle(new StartReviewCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        var result = await _disputeHandler.Handle(new DisputeCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var declaration = await _declarationRepository.GetByIdAsync(declarationId, CancellationToken.None);
        Assert.Equal(CashDeclarationStatus.Disputed, declaration!.Status);
    }

    [Fact]
    public async Task Settle_AfterApproval_Succeeds()
    {
        var declarationId = await SubmitDeclarationAsync();
        await _approveHandler.Handle(new ApproveCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        var result = await _settleHandler.Handle(new SettleCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var declaration = await _declarationRepository.GetByIdAsync(declarationId, CancellationToken.None);
        Assert.Equal(CashDeclarationStatus.Settled, declaration!.Status);
    }

    [Fact]
    public async Task Settle_BeforeApprovalOrDispute_ReturnsConflict()
    {
        var declarationId = await SubmitDeclarationAsync();

        var result = await _settleHandler.Handle(new SettleCashDeclarationCommand(Guid.NewGuid(), declarationId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
