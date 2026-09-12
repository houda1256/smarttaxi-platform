using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Commands.CancelDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class CancelDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly CancelDocumentCommandHandler _handler;

    public CancelDocumentCommandHandlerTests()
    {
        _handler = new CancelDocumentCommandHandler(_repository);
    }

    private async Task<UserDocument> UploadAsync(Guid userId)
    {
        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "storage-key", "license.pdf", "application/pdf",
            1024, "hash", issueDate: null, expirationDate: null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);
        return document;
    }

    [Fact]
    public async Task Handle_ForOwnPendingDocument_TransitionsToCancelledWithoutDeletingTheRow()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAsync(userId);

        var result = await _handler.Handle(new CancelDocumentCommand(userId, document.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(DocumentStatus.Cancelled, reloaded!.Status);
        // Metadata, hash, ownership and upload date all preserved.
        Assert.Equal(document.Sha256, reloaded.Sha256);
        Assert.Equal(document.UserId, reloaded.UserId);
        Assert.Equal(document.CreatedAt, reloaded.CreatedAt);
    }

    [Fact]
    public async Task Handle_ForSomeoneElsesDocument_ReturnsNotFound()
    {
        var owner = Guid.NewGuid();
        var document = await UploadAsync(owner);

        var result = await _handler.Handle(new CancelDocumentCommand(Guid.NewGuid(), document.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Pending, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForAlreadyApprovedDocument_ReturnsConflictAndCannotCancel()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAsync(userId);
        await _repository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);

        var result = await _handler.Handle(new CancelDocumentCommand(userId, document.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Approved, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForAlreadyCancelledDocument_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAsync(userId);
        await _handler.Handle(new CancelDocumentCommand(userId, document.Id), CancellationToken.None);

        var result = await _handler.Handle(new CancelDocumentCommand(userId, document.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
