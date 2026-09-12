using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class ProfessionalAccountRequestRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public ProfessionalAccountRequestRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentApproveAndReject_OnTheSamePendingRequest_OnlyOneAttemptSucceeds()
    {
        var request = new ProfessionalAccountRequest(Guid.NewGuid(), UserRole.Driver, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new ProfessionalAccountRequestRepository(writeContext).AddAsync(request, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new ProfessionalAccountRequestRepository(contextA);
        var repositoryB = new ProfessionalAccountRequestRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryApproveAsync(request.Id, Guid.NewGuid(), utcNow, "ok", CancellationToken.None),
            repositoryB.TryRejectAsync(request.Id, Guid.NewGuid(), utcNow, "no", null, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new ProfessionalAccountRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.True(finalState!.Status is ProfessionalAccountStatus.Approved or ProfessionalAccountStatus.Rejected);
    }

    [Fact]
    public async Task ConcurrentSuspendAndReactivate_CannotBothSucceed()
    {
        var request = new ProfessionalAccountRequest(Guid.NewGuid(), UserRole.Driver, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new ProfessionalAccountRequestRepository(writeContext);
            await repository.AddAsync(request, CancellationToken.None);
            await repository.TryApproveAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        var repositoryA = new ProfessionalAccountRequestRepository(contextA);
        var utcNow = DateTime.UtcNow;

        // Reactivate on a currently-Approved request must fail — there is
        // nothing to reactivate until it's actually suspended first.
        var reactivateBeforeSuspend = await repositoryA.TryReactivateAsync(request.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None);
        Assert.False(reactivateBeforeSuspend);

        var suspended = await repositoryA.TrySuspendAsync(request.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None);
        Assert.True(suspended);
    }
}
