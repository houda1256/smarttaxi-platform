using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Commands.RejectDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class RejectDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly RejectDocumentCommandHandler _handler;

    public RejectDocumentCommandHandlerTests()
    {
        _handler = new RejectDocumentCommandHandler(_repository);
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
    public async Task Handle_ByAuthorizedReviewer_RejectsWithReason()
    {
        var document = await UploadAsync(Guid.NewGuid());
        var reviewerId = Guid.NewGuid();

        var result = await _handler.Handle(
            new RejectDocumentCommand(reviewerId, document.Id, "Illegible scan", "please resubmit"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Rejected, reloaded!.Status);
        Assert.Equal("Illegible scan", reloaded.RejectionReason);
        Assert.Equal(reviewerId, reloaded.ReviewedBy);
    }

    [Fact]
    public async Task Handle_WithEmptyReason_ReturnsValidationError()
    {
        var document = await UploadAsync(Guid.NewGuid());

        var result = await _handler.Handle(
            new RejectDocumentCommand(Guid.NewGuid(), document.Id, "   ", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenReviewerIsTheDocumentOwner_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAsync(userId);

        var result = await _handler.Handle(
            new RejectDocumentCommand(userId, document.Id, "reason", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
