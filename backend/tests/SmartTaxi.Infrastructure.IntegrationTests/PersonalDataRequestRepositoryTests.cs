using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class PersonalDataRequestRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public PersonalDataRequestRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentCompleteAndReject_OnTheSamePendingRequest_OnlyOneAttemptSucceeds()
    {
        var request = new PersonalDataRequest(Guid.NewGuid(), PersonalDataRequestType.Export, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PersonalDataRequestRepository(writeContext).AddAsync(request, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new PersonalDataRequestRepository(contextA);
        var repositoryB = new PersonalDataRequestRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryCompleteAsync(request.Id, Guid.NewGuid(), utcNow, "done", "exports/x.json", CancellationToken.None),
            repositoryB.TryRejectAsync(request.Id, Guid.NewGuid(), utcNow, "denied", CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new PersonalDataRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.True(finalState!.Status is PersonalDataRequestStatus.Completed or PersonalDataRequestStatus.Rejected);
    }

    [Fact]
    public async Task Anonymize_PreservesUserDocumentForeignKeyIntegrity()
    {
        var email = Email.Create($"{Guid.NewGuid()}@example.com");
        var user = User.Create(email, HashedPassword.Create("hashed"), UserRole.Customer);
        var document = UserDocument.Upload(
            user.Id, DocumentType.DriverLicense, "key", "f.pdf", "application/pdf", 1, "hash", null, null, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new UserRepository(writeContext).AddAsync(user, CancellationToken.None);
            await new UserDocumentRepository(writeContext).AddAsync(document, CancellationToken.None);
        }

        await using (var mutateContext = _fixture.CreateContext())
        {
            var userRepository = new UserRepository(mutateContext);
            var reloadedUser = await userRepository.GetByIdAsync(user.Id, CancellationToken.None);
            var anonymizedEmail = Email.Create($"deleted-{Guid.NewGuid()}@anonymized.smarttaxi.invalid");
            var unusableHash = HashedPassword.Create("unusable");
            reloadedUser!.Anonymize(anonymizedEmail, unusableHash);
            reloadedUser.Deactivate();
            await userRepository.UpdateAsync(reloadedUser, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var finalUser = await new UserRepository(readContext).GetByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(finalUser);
        Assert.False(finalUser!.IsActive);
        Assert.Contains("anonymized", finalUser.Email.Value);

        // The document row survives, still pointing at the same (anonymized) user id.
        var reloadedDocument = await new UserDocumentRepository(readContext).GetByIdAsync(document.Id, CancellationToken.None);
        Assert.NotNull(reloadedDocument);
        Assert.Equal(user.Id, reloadedDocument!.UserId);
    }
}
