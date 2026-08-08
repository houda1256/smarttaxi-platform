using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class UserDocumentRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public UserDocumentRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static UserDocument NewDocument(Guid userId, DateTime? expirationDate = null) =>
        UserDocument.Upload(
            userId, DocumentType.DriverLicense, $"key-{Guid.NewGuid()}", "license.pdf", "application/pdf",
            1024, $"hash-{Guid.NewGuid()}", issueDate: null, expirationDate, DateTime.UtcNow);

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAllFields()
    {
        var document = NewDocument(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new UserDocumentRepository(writeContext).AddAsync(document, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new UserDocumentRepository(readContext).GetByIdAsync(document.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(document.UserId, reloaded!.UserId);
        Assert.Equal(document.Sha256, reloaded.Sha256);
        Assert.Equal(document.FileReference, reloaded.FileReference);
        Assert.Equal(DocumentStatus.Pending, reloaded.Status);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyThatUsersDocuments()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new UserDocumentRepository(writeContext);
            await repository.AddAsync(NewDocument(userA), CancellationToken.None);
            await repository.AddAsync(NewDocument(userA), CancellationToken.None);
            await repository.AddAsync(NewDocument(userB), CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var documentsForA = await new UserDocumentRepository(readContext).GetForUserAsync(userA, CancellationToken.None);

        Assert.Equal(2, documentsForA.Count);
        Assert.All(documentsForA, d => Assert.Equal(userA, d.UserId));
    }

    [Fact]
    public async Task ConcurrentApproveAndReject_OnTheSamePendingDocument_OnlyOneAttemptSucceeds()
    {
        var document = NewDocument(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new UserDocumentRepository(writeContext).AddAsync(document, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new UserDocumentRepository(contextA);
        var repositoryB = new UserDocumentRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryApproveAsync(document.Id, Guid.NewGuid(), utcNow, "approved", CancellationToken.None),
            repositoryB.TryRejectAsync(document.Id, Guid.NewGuid(), utcNow, "rejected", null, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new UserDocumentRepository(readContext).GetByIdAsync(document.Id, CancellationToken.None);
        Assert.True(finalState!.Status is DocumentStatus.Approved or DocumentStatus.Rejected);
    }

    [Fact]
    public async Task ConcurrentCancelAndApprove_OnTheSamePendingDocument_OnlyOneAttemptSucceeds()
    {
        var userId = Guid.NewGuid();
        var document = NewDocument(userId);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new UserDocumentRepository(writeContext).AddAsync(document, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new UserDocumentRepository(contextA);
        var repositoryB = new UserDocumentRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            repositoryA.TryCancelAsync(document.Id, userId, utcNow, CancellationToken.None),
            repositoryB.TryApproveAsync(document.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var finalState = await new UserDocumentRepository(readContext).GetByIdAsync(document.Id, CancellationToken.None);
        Assert.True(finalState!.Status is DocumentStatus.Cancelled or DocumentStatus.Approved);
    }

    [Fact]
    public async Task ConcurrentReplace_OnTheSameDocument_OnlyOneAttemptSucceeds()
    {
        var document = NewDocument(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new UserDocumentRepository(writeContext).AddAsync(document, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new UserDocumentRepository(contextA);
        var repositoryB = new UserDocumentRepository(contextB);
        var utcNow = DateTime.UtcNow;

        var replacementA = document.CreateReplacement("key-a", "a.pdf", "application/pdf", 1, "hash-a", null, null, utcNow);
        var replacementB = document.CreateReplacement("key-b", "b.pdf", "application/pdf", 1, "hash-b", null, null, utcNow);

        var results = await Task.WhenAll(
            repositoryA.TryReplaceAsync(document, replacementA, utcNow, CancellationToken.None),
            repositoryB.TryReplaceAsync(document, replacementB, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var readRepository = new UserDocumentRepository(readContext);
        var original = await readRepository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Replaced, original!.Status);

        var childCount = (await readRepository.GetForUserAsync(document.UserId, CancellationToken.None))
            .Count(d => d.ReplacesDocumentId == document.Id);
        Assert.Equal(1, childCount); // only the winning replacement was ever inserted
    }

    [Fact]
    public async Task ExpireDueDocumentsAsync_MarksOverdueApprovedDocumentsExpired_ButLeavesOthersUntouched()
    {
        var utcNow = DateTime.UtcNow;
        var overdue = NewDocument(Guid.NewGuid(), utcNow.AddDays(-1));
        var notYetDue = NewDocument(Guid.NewGuid(), utcNow.AddDays(30));

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new UserDocumentRepository(writeContext);
            await repository.AddAsync(overdue, CancellationToken.None);
            await repository.AddAsync(notYetDue, CancellationToken.None);
            await repository.TryApproveAsync(overdue.Id, Guid.NewGuid(), utcNow.AddDays(-30), null, CancellationToken.None);
            await repository.TryApproveAsync(notYetDue.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None);
        }

        await using (var sweepContext = _fixture.CreateContext())
        {
            var count = await new UserDocumentRepository(sweepContext).ExpireDueDocumentsAsync(utcNow, CancellationToken.None);
            Assert.True(count >= 1);
        }

        await using var readContext = _fixture.CreateContext();
        var readRepository = new UserDocumentRepository(readContext);
        Assert.Equal(DocumentStatus.Expired, (await readRepository.GetByIdAsync(overdue.Id, CancellationToken.None))!.Status);
        Assert.Equal(DocumentStatus.Approved, (await readRepository.GetByIdAsync(notYetDue.Id, CancellationToken.None))!.Status);
    }
}
