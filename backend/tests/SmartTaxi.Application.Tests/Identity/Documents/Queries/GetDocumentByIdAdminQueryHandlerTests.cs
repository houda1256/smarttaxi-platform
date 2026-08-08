using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Queries.GetDocumentByIdAdmin;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Queries;

public class GetDocumentByIdAdminQueryHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly FakeDocumentAccessAuditRepository _auditRepository = new();
    private readonly GetDocumentByIdAdminQueryHandler _handler;

    public GetDocumentByIdAdminQueryHandlerTests()
    {
        _handler = new GetDocumentByIdAdminQueryHandler(_repository, _auditRepository, new FakeDocumentExpirationPolicy());
    }

    [Fact]
    public async Task Handle_ForAnotherUsersDocument_ReturnsItAndCreatesAuditRecord()
    {
        var owner = Guid.NewGuid();
        var reviewer = Guid.NewGuid();
        var document = UserDocument.Upload(
            owner, DocumentType.DriverLicense, "key", "f.pdf", "application/pdf", 1, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        var result = await _handler.Handle(
            new GetDocumentByIdAdminQuery(reviewer, document.Id, "203.0.113.5"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_auditRepository.Entries);
        var entry = _auditRepository.Entries.Single();
        Assert.Equal(document.Id, entry.DocumentId);
        Assert.Equal(reviewer, entry.AccessedByUserId);
        Assert.Equal(DocumentAccessType.MetadataRead, entry.AccessType);
    }

    [Fact]
    public async Task Handle_ForUnknownDocument_ReturnsNotFoundAndCreatesNoAuditRecord()
    {
        var result = await _handler.Handle(
            new GetDocumentByIdAdminQuery(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(_auditRepository.Entries);
    }
}
