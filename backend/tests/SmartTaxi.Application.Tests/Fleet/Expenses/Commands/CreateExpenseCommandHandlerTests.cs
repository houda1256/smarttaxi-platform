using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Tests.Fleet.Expenses.Commands;

public class CreateExpenseCommandHandlerTests
{
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly FakeFleetExpenseRepository _repository = new();
    private readonly CreateExpenseCommandHandler _handler;

    public CreateExpenseCommandHandlerTests()
    {
        _handler = new CreateExpenseCommandHandler(_fleetRepository, _repository);
    }

    [Fact]
    public async Task Handle_WithoutFleet_CreatesDraftExpense()
    {
        var command = new CreateExpenseCommand(
            Guid.NewGuid(), null, Guid.NewGuid(), null, ExpenseCategory.Fuel, 45.50m, "MAD",
            new DateOnly(2026, 1, 15), "Plein d'essence", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var expense = await _repository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(ExpenseStatus.Draft, expense!.Status);
    }

    [Fact]
    public async Task Handle_WithNegativeAmount_ReturnsValidationError()
    {
        var command = new CreateExpenseCommand(
            Guid.NewGuid(), null, Guid.NewGuid(), null, ExpenseCategory.Fuel, -10m, "MAD",
            new DateOnly(2026, 1, 15), null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithFleetNotOwnedByRequestingOwner_ReturnsNotFound()
    {
        var otherOwnerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(otherOwnerId, "Fleet A", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);

        var command = new CreateExpenseCommand(
            Guid.NewGuid(), fleet.Id, null, null, ExpenseCategory.Maintenance, 100m, "MAD",
            new DateOnly(2026, 1, 15), null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
