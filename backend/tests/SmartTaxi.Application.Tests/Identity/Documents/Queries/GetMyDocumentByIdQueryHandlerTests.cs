using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents.Queries.GetMyDocumentById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Queries;

public class GetMyDocumentByIdQueryHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly GetMyDocumentByIdQueryHandler _handler;

    public GetMyDocumentByIdQueryHandlerTests()
    {
        _handler = new GetMyDocumentByIdQueryHandler(_repository, new FakeDocumentExpirationPolicy());
    }

    [Fact]
    public async Task Handle_ForOwnDocument_ReturnsIt()
    {
        var userId = Guid.NewGuid();
        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "key", "f.pdf", "application/pdf", 1, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        var result = await _handler.Handle(new GetMyDocumentByIdQuery(userId, document.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(document.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ForAnotherUsersDocument_ReturnsNotFound()
    {
        var owner = Guid.NewGuid();
        var document = UserDocument.Upload(
            owner, DocumentType.DriverLicense, "key", "f.pdf", "application/pdf", 1, "hash", null, null, DateTime.UtcNow);
        await _repository.AddAsync(document, CancellationToken.None);

        var result = await _handler.Handle(new GetMyDocumentByIdQuery(Guid.NewGuid(), document.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForUnknownDocument_ReturnsNotFound()
    {
        var result = await _handler.Handle(
            new GetMyDocumentByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
