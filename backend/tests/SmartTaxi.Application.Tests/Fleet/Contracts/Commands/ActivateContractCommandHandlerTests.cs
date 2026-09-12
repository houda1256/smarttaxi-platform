using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Contracts.Commands.ActivateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.CreateContract;
using SmartTaxi.Application.Fleet.Contracts.Commands.SubmitContract;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Contracts.Commands;

public class ActivateContractCommandHandlerTests
{
    private readonly FakeDriverOwnerContractRepository _repository = new();
    private readonly CreateContractCommandHandler _createHandler;
    private readonly SubmitContractCommandHandler _submitHandler;
    private readonly ActivateContractCommandHandler _activateHandler;

    public ActivateContractCommandHandlerTests()
    {
        _createHandler = new CreateContractCommandHandler(_repository);
        _submitHandler = new SubmitContractCommandHandler(_repository);
        _activateHandler = new ActivateContractCommandHandler(_repository);
    }

    private async Task<(Guid OwnerId, Guid ContractId)> CreateAndSubmitAsync(string? documentReference)
    {
        var ownerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateContractCommand(
                ownerId, Guid.NewGuid(), null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
                1000, null, null, PaymentFrequency.Monthly, documentReference),
            CancellationToken.None);

        await _submitHandler.Handle(new SubmitContractCommand(ownerId, createResult.Value), CancellationToken.None);

        return (ownerId, createResult.Value);
    }

    [Fact]
    public async Task Handle_WithoutSignedDocumentReference_ReturnsValidationError()
    {
        var (ownerId, contractId) = await CreateAndSubmitAsync(null);

        var result = await _activateHandler.Handle(new ActivateContractCommand(ownerId, contractId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithSignedDocumentReference_ActivatesContract()
    {
        var (ownerId, contractId) = await CreateAndSubmitAsync("signed-doc-ref");

        var result = await _activateHandler.Handle(new ActivateContractCommand(ownerId, contractId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var contract = await _repository.GetByIdAsync(contractId, CancellationToken.None);
        Assert.Equal(ContractStatus.Active, contract!.Status);
    }
}
