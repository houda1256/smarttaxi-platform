using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Commands.ReplaceDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class ReplaceDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly FakeFileStorageService _fileStorage = new();
    private readonly FakeDocumentUploadPolicy _policy = new();
    private readonly ReplaceDocumentCommandHandler _handler;

    public ReplaceDocumentCommandHandlerTests()
    {
        _handler = new ReplaceDocumentCommandHandler(_repository, _fileStorage, new DocumentUploadValidator(_policy));
    }

    private async Task<UserDocument> UploadAsync(Guid userId, DateTime utcNow)
    {
        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "storage-key-1", "license.pdf", "application/pdf",
            1024, "hash-1", issueDate: null, expirationDate: null, utcNow);
        await _repository.AddAsync(document, CancellationToken.None);
        return document;
    }

    [Fact]
    public async Task Handle_ForApprovedDocument_CreatesNewPendingVersionAndPreservesOriginalReviewHistory()
    {
        var userId = Guid.NewGuid();
        var reviewedAt = DateTime.UtcNow;
        var original = await UploadAsync(userId, reviewedAt);
        await _repository.TryApproveAsync(original.Id, Guid.NewGuid(), reviewedAt, "looks good", CancellationToken.None);

        var command = new ReplaceDocumentCommand(
            userId, original.Id, new MemoryStream([9, 9, 9]), "license-v2.pdf", "application/pdf", null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.NewVersion);

        var oldVersion = await _repository.GetByIdAsync(original.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Replaced, oldVersion!.Status);
        // The original approval is never mutated — history preserved by construction.
        Assert.NotNull(oldVersion.ReviewedAt);
        Assert.Equal("looks good", oldVersion.ReviewComment);

        var newVersion = await _repository.GetByIdAsync(result.Value.NewDocumentId, CancellationToken.None);
        Assert.Equal(DocumentStatus.Pending, newVersion!.Status);
        Assert.Equal(original.Id, newVersion.ReplacesDocumentId);
    }

    [Fact]
    public async Task Handle_ForDocumentOwnedBySomeoneElse_ReturnsNotFound()
    {
        var owner = Guid.NewGuid();
        var original = await UploadAsync(owner, DateTime.UtcNow);

        var command = new ReplaceDocumentCommand(
            Guid.NewGuid(), original.Id, new MemoryStream([1]), "x.pdf", "application/pdf", null, null);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForAlreadyReplacedDocument_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var original = await UploadAsync(userId, DateTime.UtcNow);
        await _handler.Handle(
            new ReplaceDocumentCommand(userId, original.Id, new MemoryStream([1]), "a.pdf", "application/pdf", null, null),
            CancellationToken.None);

        var secondReplace = await _handler.Handle(
            new ReplaceDocumentCommand(userId, original.Id, new MemoryStream([2]), "b.pdf", "application/pdf", null, null),
            CancellationToken.None);

        Assert.False(secondReplace.IsSuccess);
        Assert.Equal(ErrorType.Conflict, secondReplace.ErrorType);
    }
}
