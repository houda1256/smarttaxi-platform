using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Commands.SuspendDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class SuspendDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly SuspendDocumentCommandHandler _handler;

    public SuspendDocumentCommandHandlerTests()
    {
        _handler = new SuspendDocumentCommandHandler(_repository);
    }

    private async Task<UserDocument> UploadAndApproveAsync(Guid userId)
    {
        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "storage-key", "license.pdf", "application/pdf",
            1024, "hash", issueDate: null, expirationDate: null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);
        await _repository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        return document;
    }

    [Fact]
    public async Task Handle_ForApprovedDocument_SuspendsItAndMakesItNoLongerValid()
    {
        var document = await UploadAndApproveAsync(Guid.NewGuid());

        var result = await _handler.Handle(
            new SuspendDocumentCommand(Guid.NewGuid(), document.Id, "fraud suspected"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Suspended, reloaded!.Status);
        Assert.False(reloaded.IsCurrentlyValid(DateTime.UtcNow));
    }

    [Fact]
    public async Task Handle_ForPendingDocument_ReturnsConflict()
    {
        var document = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "key", "f.pdf", "application/pdf",
            1, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        var result = await _handler.Handle(
            new SuspendDocumentCommand(Guid.NewGuid(), document.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenReviewerIsTheDocumentOwner_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var document = await UploadAndApproveAsync(userId);

        var result = await _handler.Handle(new SuspendDocumentCommand(userId, document.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
