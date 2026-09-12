using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Contracts.Commands;

public class CreateContractCommandHandlerTests
{
    private readonly FakeDriverOwnerContractRepository _repository = new();
    private readonly CreateContractCommandHandler _handler;
    private readonly SubmitContractCommandHandler _submitHandler;

    public CreateContractCommandHandlerTests()
    {
        _handler = new CreateContractCommandHandler(_repository);
        _submitHandler = new SubmitContractCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidPercentageSplit_CreatesDraftContract()
    {
        var command = new CreateContractCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 70, 30, PaymentFrequency.PerRide, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var contract = await _repository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(ContractStatus.Draft, contract!.Status);
    }

    [Fact]
    public async Task Handle_WithPercentagesNotSummingTo100_ReturnsValidationError()
    {
        var command = new CreateContractCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 60, 30, PaymentFrequency.PerRide, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenActiveContractAlreadyExistsForDriverOwnerPair_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var first = new CreateContractCommand(
            ownerId, driverId, null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            1000, null, null, PaymentFrequency.Monthly, null);
        var firstResult = await _handler.Handle(first, CancellationToken.None);
        await _submitHandler.Handle(new SubmitContractCommand(ownerId, firstResult.Value), CancellationToken.None);

        var second = new CreateContractCommand(
            ownerId, driverId, null, ContractType.FixedSalary, new DateOnly(2026, 2, 1), null,
            1200, null, null, PaymentFrequency.Monthly, null);
        var result = await _handler.Handle(second, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
