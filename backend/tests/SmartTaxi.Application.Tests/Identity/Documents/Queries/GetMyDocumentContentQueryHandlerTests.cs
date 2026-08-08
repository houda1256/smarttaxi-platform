using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentContent;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Queries;

public class GetMyDocumentContentQueryHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly FakeDocumentAccessAuditRepository _auditRepository = new();
    private readonly FakeFileStorageService _fileStorage = new();
    private readonly GetMyDocumentContentQueryHandler _handler;

    public GetMyDocumentContentQueryHandlerTests()
    {
        _handler = new GetMyDocumentContentQueryHandler(_repository, _auditRepository, _fileStorage);
    }

    [Fact]
    public async Task Handle_ForOwnDocument_ReturnsContentAndCreatesAuditRecord()
    {
        var userId = Guid.NewGuid();
        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "storage-key", "f.pdf", "application/pdf", 3, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);
        await _fileStorage.SaveAsync("storage-key", new MemoryStream([1, 2, 3]), CancellationToken.None);

        var result = await _handler.Handle(
            new GetMyDocumentContentQuery(userId, document.Id, "203.0.113.5"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value!.MimeType);
        Assert.Single(_auditRepository.Entries);
        Assert.Equal(DocumentAccessType.ContentDownload, _auditRepository.Entries.Single().AccessType);
    }

    [Fact]
    public async Task Handle_ForAnotherUsersDocument_ReturnsNotFoundAndCreatesNoAuditRecord()
    {
        var owner = Guid.NewGuid();
        var document = UserDocument.Upload(
            owner, DocumentType.DriverLicense, "storage-key", "f.pdf", "application/pdf", 3, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        var result = await _handler.Handle(
            new GetMyDocumentContentQuery(Guid.NewGuid(), document.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Empty(_auditRepository.Entries);
    }
}
