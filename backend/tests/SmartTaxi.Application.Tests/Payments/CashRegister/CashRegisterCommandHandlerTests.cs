using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.CashRegister.Commands.CloseCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.DisputeCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.OpenCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.ReconcileCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.RecordCashMovement;
using SmartTaxi.Application.Payments.CashRegister.Commands.RegisterCashRegister;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Tests.Payments.CashRegister;

public class CashRegisterCommandHandlerTests
{
    private readonly FakeCashRegisterRepository _registerRepository = new();
    private readonly FakeCashRegisterSessionRepository _sessionRepository = new();
    private readonly FakeCashMovementRepository _movementRepository = new();

    private readonly RegisterCashRegisterCommandHandler _registerHandler;
    private readonly OpenCashRegisterSessionCommandHandler _openHandler;
    private readonly RecordCashMovementCommandHandler _recordMovementHandler;
    private readonly CloseCashRegisterSessionCommandHandler _closeHandler;
    private readonly ReconcileCashRegisterSessionCommandHandler _reconcileHandler;
    private readonly DisputeCashRegisterSessionCommandHandler _disputeHandler;

    public CashRegisterCommandHandlerTests()
    {
        _registerHandler = new RegisterCashRegisterCommandHandler(_registerRepository);
        _openHandler = new OpenCashRegisterSessionCommandHandler(_registerRepository, _sessionRepository);
        _recordMovementHandler = new RecordCashMovementCommandHandler(_sessionRepository, _movementRepository);
        _closeHandler = new CloseCashRegisterSessionCommandHandler(_sessionRepository, _movementRepository);
        _reconcileHandler = new ReconcileCashRegisterSessionCommandHandler(_sessionRepository);
        _disputeHandler = new DisputeCashRegisterSessionCommandHandler(_sessionRepository);
    }

    private async Task<Guid> RegisterAndOpenSessionAsync(Guid ownerId, decimal openingBalance)
    {
        var registerResult = await _registerHandler.Handle(new RegisterCashRegisterCommand(ownerId, "Caisse principale"), CancellationToken.None);
        var openResult = await _openHandler.Handle(
            new OpenCashRegisterSessionCommand(registerResult.Value, ownerId, openingBalance), CancellationToken.None);
        return openResult.Value;
    }

    [Fact]
    public async Task OpenSession_WhileAnotherIsOpen_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var registerResult = await _registerHandler.Handle(new RegisterCashRegisterCommand(ownerId, "Caisse"), CancellationToken.None);
        await _openHandler.Handle(new OpenCashRegisterSessionCommand(registerResult.Value, ownerId, 100m), CancellationToken.None);

        var second = await _openHandler.Handle(new OpenCashRegisterSessionCommand(registerResult.Value, ownerId, 50m), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task RecordMovement_OnOpenSession_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);

        var result = await _recordMovementHandler.Handle(
            new RecordCashMovementCommand(sessionId, CashMovementType.RideIncome, 25m, "Course #1", ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RecordMovement_OnClosedSession_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);
        await _closeHandler.Handle(new CloseCashRegisterSessionCommand(sessionId, ownerId, 100m, null), CancellationToken.None);

        var result = await _recordMovementHandler.Handle(
            new RecordCashMovementCommand(sessionId, CashMovementType.RideIncome, 25m, null, ownerId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CloseSession_WithMatchingBalance_RequiresNoReason()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);
        await _recordMovementHandler.Handle(new RecordCashMovementCommand(sessionId, CashMovementType.RideIncome, 50m, null, ownerId), CancellationToken.None);
        await _recordMovementHandler.Handle(new RecordCashMovementCommand(sessionId, CashMovementType.Expense, -10m, null, ownerId), CancellationToken.None);

        var result = await _closeHandler.Handle(new CloseCashRegisterSessionCommand(sessionId, ownerId, 140m, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }

    [Fact]
    public async Task CloseSession_WithDifferenceAndNoReason_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);

        var result = await _closeHandler.Handle(new CloseCashRegisterSessionCommand(sessionId, ownerId, 90m, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CloseSession_WithDifferenceAndReason_SucceedsAndReportsDifference()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);

        var result = await _closeHandler.Handle(
            new CloseCashRegisterSessionCommand(sessionId, ownerId, 90m, "Erreur de comptage"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(-10m, result.Value);
    }

    [Fact]
    public async Task ReconcileSession_AfterClose_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);
        await _closeHandler.Handle(new CloseCashRegisterSessionCommand(sessionId, ownerId, 100m, null), CancellationToken.None);

        var result = await _reconcileHandler.Handle(new ReconcileCashRegisterSessionCommand(sessionId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DisputeSession_WithoutReason_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var sessionId = await RegisterAndOpenSessionAsync(ownerId, 100m);
        await _closeHandler.Handle(new CloseCashRegisterSessionCommand(sessionId, ownerId, 100m, null), CancellationToken.None);

        var result = await _disputeHandler.Handle(new DisputeCashRegisterSessionCommand(sessionId, Guid.NewGuid(), ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
