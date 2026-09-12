using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class DriverOwnerContractRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public DriverOwnerContractRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static DriverOwnerContract NewActiveLikeContract(Guid ownerId, Guid driverId, ContractStatus status)
    {
        var contract = DriverOwnerContract.CreateDraft(
            ownerId, driverId, null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null, 1000, null, null,
            PaymentFrequency.Monthly, "signed-ref", DateTime.UtcNow);

        typeof(DriverOwnerContract).GetProperty(nameof(DriverOwnerContract.Status))!.SetValue(contract, status);
        return contract;
    }

    [Fact]
    public async Task ConcurrentAddAsync_TwoActiveContractsForSameDriverOwnerPair_OnlyOneSucceeds_EnforcedByFilteredUniqueIndex()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var contractA = NewActiveLikeContract(ownerId, driverId, ContractStatus.Active);
        var contractB = NewActiveLikeContract(ownerId, driverId, ContractStatus.PendingSignature);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            TryAddAsync(new DriverOwnerContractRepository(contextA), contractA),
            TryAddAsync(new DriverOwnerContractRepository(contextB), contractB));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.DriverOwnerContracts.CountAsync(c => c.DriverId == driverId && c.OwnerId == ownerId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_DraftAndActiveContractsForSamePair_BothSucceed_DraftIsNotConstrainedByFilteredIndex()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var draft = NewActiveLikeContract(ownerId, driverId, ContractStatus.Draft);
        var active = NewActiveLikeContract(ownerId, driverId, ContractStatus.Active);

        await using var writeContext = _fixture.CreateContext();
        var repository = new DriverOwnerContractRepository(writeContext);
        await repository.AddAsync(draft, CancellationToken.None);
        await repository.AddAsync(active, CancellationToken.None);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.DriverOwnerContracts.CountAsync(c => c.DriverId == driverId && c.OwnerId == ownerId);
        Assert.Equal(2, count);
    }

    private static async Task<bool> TryAddAsync(DriverOwnerContractRepository repository, DriverOwnerContract contract)
    {
        try
        {
            await repository.AddAsync(contract, CancellationToken.None);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return false;
        }
    }
}
