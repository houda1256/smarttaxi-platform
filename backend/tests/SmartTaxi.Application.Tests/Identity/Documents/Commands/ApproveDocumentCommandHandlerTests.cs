using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Commands.ApproveDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class ApproveDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly ApproveDocumentCommandHandler _handler;

    public ApproveDocumentCommandHandlerTests()
    {
        _handler = new ApproveDocumentCommandHandler(_repository);
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
    public async Task Handle_ByAuthorizedReviewer_ApprovesPendingDocument()
    {
        var document = await UploadAsync(Guid.NewGuid());
        var reviewerId = Guid.NewGuid();

        var result = await _handler.Handle(
            new ApproveDocumentCommand(reviewerId, document.Id, "all good"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Approved, reloaded!.Status);
        Assert.Equal(reviewerId, reloaded.ReviewedBy);
        Assert.Equal("all good", reloaded.ReviewComment);
    }

    [Fact]
    public async Task Handle_WhenReviewerIsTheDocumentOwner_ReturnsForbiddenAndDoesNotApprove()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAsync(userId);

        var result = await _handler.Handle(new ApproveDocumentCommand(userId, document.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Pending, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForAlreadyRejectedDocument_ReturnsConflict()
    {
        var document = await UploadAsync(Guid.NewGuid());
        await _repository.TryRejectAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, "bad scan", null, CancellationToken.None);

        var result = await _handler.Handle(
            new ApproveDocumentCommand(Guid.NewGuid(), document.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForUnknownDocument_ReturnsNotFound()
    {
        var result = await _handler.Handle(
            new ApproveDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
