using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDriverOwnerContractRepository : IDriverOwnerContractRepository
{
    private readonly Dictionary<Guid, DriverOwnerContract> _contractsById = new();

    private static readonly ContractStatus[] ActiveOrPendingStatuses =
        [ContractStatus.PendingSignature, ContractStatus.Active];

    public Task AddAsync(DriverOwnerContract contract, CancellationToken cancellationToken)
    {
        _contractsById[contract.Id] = contract;
        return Task.CompletedTask;
    }

    public Task<DriverOwnerContract?> GetByIdAsync(Guid contractId, CancellationToken cancellationToken) =>
        Task.FromResult(_contractsById.GetValueOrDefault(contractId));

    public Task<DriverOwnerContract?> GetActiveOrPendingForDriverOwnerAsync(
        Guid driverId, Guid ownerId, CancellationToken cancellationToken)
    {
        var contract = _contractsById.Values.FirstOrDefault(c =>
            c.DriverId == driverId && c.OwnerId == ownerId && ActiveOrPendingStatuses.Contains(c.Status));
        return Task.FromResult(contract);
    }

    public Task<DriverOwnerContract?> GetActiveForDriverAndOwnerAsync(Guid driverId, Guid ownerId, CancellationToken cancellationToken)
    {
        var contract = _contractsById.Values.FirstOrDefault(c =>
            c.DriverId == driverId && c.OwnerId == ownerId && c.Status == ContractStatus.Active);
        return Task.FromResult(contract);
    }

    public Task<IReadOnlyCollection<DriverOwnerContract>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverOwnerContract> contracts = _contractsById.Values.Where(c => c.OwnerId == ownerId).ToList();
        return Task.FromResult(contracts);
    }

    public Task<IReadOnlyCollection<DriverOwnerContract>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DriverOwnerContract> contracts = _contractsById.Values.Where(c => c.DriverId == driverId).ToList();
        return Task.FromResult(contracts);
    }

    public Task UpdateAsync(DriverOwnerContract contract, CancellationToken cancellationToken)
    {
        _contractsById[contract.Id] = contract;
        return Task.CompletedTask;
    }

    public Task<bool> TrySubmitAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(contractId, ContractStatus.Draft, ContractStatus.PendingSignature, utcNow);

    public Task<bool> TryActivateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(contractId, ContractStatus.PendingSignature, ContractStatus.Active, utcNow);

    public Task<bool> TrySuspendAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(contractId, ContractStatus.Active, ContractStatus.Suspended, utcNow);

    public Task<bool> TryTerminateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_contractsById.TryGetValue(contractId, out var contract)
            || contract.Status is not (ContractStatus.Active or ContractStatus.Suspended))
        {
            return Task.FromResult(false);
        }

        SetProperty(contract, nameof(DriverOwnerContract.Status), ContractStatus.Terminated);
        SetProperty(contract, nameof(DriverOwnerContract.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransition(Guid contractId, ContractStatus from, ContractStatus to, DateTime utcNow)
    {
        if (!_contractsById.TryGetValue(contractId, out var contract) || contract.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(contract, nameof(DriverOwnerContract.Status), to);
        SetProperty(contract, nameof(DriverOwnerContract.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(DriverOwnerContract contract, string propertyName, object? value) =>
        typeof(DriverOwnerContract).GetProperty(propertyName)!.SetValue(contract, value);
}
