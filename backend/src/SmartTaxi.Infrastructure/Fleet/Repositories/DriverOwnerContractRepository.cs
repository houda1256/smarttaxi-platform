using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class DriverOwnerContractRepository : IDriverOwnerContractRepository
{
    private static readonly ContractStatus[] ActiveOrPendingStatuses = [ContractStatus.PendingSignature, ContractStatus.Active];

    private readonly ApplicationDbContext _context;

    public DriverOwnerContractRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DriverOwnerContract contract, CancellationToken cancellationToken)
    {
        await _context.DriverOwnerContracts.AddAsync(contract, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<DriverOwnerContract?> GetByIdAsync(Guid contractId, CancellationToken cancellationToken) =>
        _context.DriverOwnerContracts.FirstOrDefaultAsync(contract => contract.Id == contractId, cancellationToken);

    public Task<DriverOwnerContract?> GetActiveOrPendingForDriverOwnerAsync(
        Guid driverId, Guid ownerId, CancellationToken cancellationToken) =>
        _context.DriverOwnerContracts.FirstOrDefaultAsync(
            contract => contract.DriverId == driverId && contract.OwnerId == ownerId
                && ActiveOrPendingStatuses.Contains(contract.Status), cancellationToken);

    public Task<DriverOwnerContract?> GetActiveForDriverAndOwnerAsync(Guid driverId, Guid ownerId, CancellationToken cancellationToken) =>
        _context.DriverOwnerContracts.FirstOrDefaultAsync(
            contract => contract.DriverId == driverId && contract.OwnerId == ownerId && contract.Status == ContractStatus.Active,
            cancellationToken);

    public async Task<IReadOnlyCollection<DriverOwnerContract>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _context.DriverOwnerContracts.Where(contract => contract.OwnerId == ownerId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DriverOwnerContract>> GetForDriverAsync(Guid driverId, CancellationToken cancellationToken) =>
        await _context.DriverOwnerContracts.Where(contract => contract.DriverId == driverId).ToListAsync(cancellationToken);

    public Task UpdateAsync(DriverOwnerContract contract, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<bool> TrySubmitAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(contractId, ContractStatus.Draft, ContractStatus.PendingSignature, utcNow, cancellationToken);

    public Task<bool> TryActivateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(contractId, ContractStatus.PendingSignature, ContractStatus.Active, utcNow, cancellationToken);

    public Task<bool> TrySuspendAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(contractId, ContractStatus.Active, ContractStatus.Suspended, utcNow, cancellationToken);

    public async Task<bool> TryTerminateAsync(Guid contractId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverOwnerContracts
            .Where(contract => contract.Id == contractId
                && (contract.Status == ContractStatus.Active || contract.Status == ContractStatus.Suspended))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(contract => contract.Status, ContractStatus.Terminated)
                .SetProperty(contract => contract.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    private async Task<bool> TryTransitionAsync(
        Guid contractId, ContractStatus from, ContractStatus to, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.DriverOwnerContracts
            .Where(contract => contract.Id == contractId && contract.Status == from)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(contract => contract.Status, to)
                .SetProperty(contract => contract.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
