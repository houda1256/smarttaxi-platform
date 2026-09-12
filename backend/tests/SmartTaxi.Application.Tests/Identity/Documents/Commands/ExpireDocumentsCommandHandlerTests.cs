using SmartTaxi.Application.Identity.Documents.Commands.ExpireDocuments;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class ExpireDocumentsCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly ExpireDocumentsCommandHandler _handler;

    public ExpireDocumentsCommandHandlerTests()
    {
        _handler = new ExpireDocumentsCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ForApprovedDocumentPastExpiration_MarksItExpired()
    {
        var utcNow = DateTime.UtcNow;
        var document = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "key", "f.pdf", "application/pdf",
            1, "hash", null, utcNow.AddDays(-1), utcNow.AddDays(-10));
        await _repository.AddAsync(document, CancellationToken.None);
        await _repository.TryApproveAsync(document.Id, Guid.NewGuid(), utcNow.AddDays(-10), null, CancellationToken.None);

        var result = await _handler.Handle(new ExpireDocumentsCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.ExpiredCount);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Expired, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForApprovedDocumentNotYetExpired_LeavesItApproved()
    {
        var utcNow = DateTime.UtcNow;
        var document = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "key", "f.pdf", "application/pdf",
            1, "hash", null, utcNow.AddDays(30), utcNow);
        await _repository.AddAsync(document, CancellationToken.None);
        await _repository.TryApproveAsync(document.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None);

        var result = await _handler.Handle(new ExpireDocumentsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Value!.ExpiredCount);
        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Approved, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForPendingDocumentPastAnExpirationDate_DoesNotTouchIt()
    {
        var utcNow = DateTime.UtcNow;
        var document = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "key", "f.pdf", "application/pdf",
            1, "hash", null, utcNow.AddDays(-1), utcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        await _handler.Handle(new ExpireDocumentsCommand(), CancellationToken.None);

        var reloaded = await _repository.GetByIdAsync(document.Id, CancellationToken.None);
        Assert.Equal(DocumentStatus.Pending, reloaded!.Status);
    }
}
