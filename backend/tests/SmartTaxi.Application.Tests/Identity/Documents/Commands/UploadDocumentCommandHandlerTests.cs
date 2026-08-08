using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Commands.UploadDocument;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Identity.Documents.Commands;

public class UploadDocumentCommandHandlerTests
{
    private readonly FakeUserDocumentRepository _repository = new();
    private readonly FakeFileStorageService _fileStorage = new();
    private readonly FakeDocumentUploadPolicy _policy = new();
    private readonly UploadDocumentCommandHandler _handler;

    public UploadDocumentCommandHandlerTests()
    {
        _handler = new UploadDocumentCommandHandler(_repository, _fileStorage, new DocumentUploadValidator(_policy));
    }

    private static UploadDocumentCommand MakeCommand(
        Guid? userId = null, byte[]? content = null, string mimeType = "application/pdf",
        string fileName = "license.pdf", DocumentType documentType = DocumentType.DriverLicense) =>
        new(
            userId ?? Guid.NewGuid(),
            documentType,
            new MemoryStream(content ?? [1, 2, 3, 4]),
            fileName,
            mimeType,
            IssueDate: null,
            ExpirationDate: null);

    [Fact]
    public async Task Handle_WithAllowedMimeTypeAndSize_UploadsAsPending()
    {
        var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentStatus.Pending, result.Value!.Status);
        Assert.Equal(1, _repository.Count);
        Assert.Equal(1, _fileStorage.SavedFileCount);
    }

    [Fact]
    public async Task Handle_WithUnsupportedMimeType_ReturnsValidationError()
    {
        var result = await _handler.Handle(MakeCommand(mimeType: "application/x-msdownload"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Handle_WithOversizedFile_ReturnsValidationError()
    {
        var oversized = new byte[_policy.MaxFileSizeBytes + 1];
        var result = await _handler.Handle(MakeCommand(content: oversized), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(0, _repository.Count);
    }

    [Fact]
    public async Task Handle_StoresContentUnderAKeyThatDoesNotContainTheOriginalFileName()
    {
        var maliciousName = "../../etc/passwd.pdf";
        var userId = Guid.NewGuid();
        await _handler.Handle(MakeCommand(userId: userId, fileName: maliciousName), CancellationToken.None);

        var document = (await _repository.GetForUserAsync(userId, CancellationToken.None)).Single();

        Assert.DoesNotContain("passwd", document.FileReference);
        Assert.DoesNotContain("..", document.FileReference);
        // Display name is sanitized (path separators stripped) but still not
        // used to construct the storage path/key above.
        Assert.DoesNotContain("/", document.FileName);
        Assert.Contains("passwd", document.FileName);
    }

    [Fact]
    public async Task Handle_ComputesAndStoresSha256OfActualContent()
    {
        var content = new byte[] { 10, 20, 30 };
        var expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));
        var userId = Guid.NewGuid();

        await _handler.Handle(MakeCommand(userId: userId, content: content), CancellationToken.None);

        var document = (await _repository.GetForUserAsync(userId, CancellationToken.None)).Single();
        Assert.Equal(expectedHash, document.Sha256);
    }

    [Fact]
    public async Task Handle_WithDuplicateHashForSameUserAndType_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };

        var first = await _handler.Handle(MakeCommand(userId: userId, content: content), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(MakeCommand(userId: userId, content: content), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        Assert.Equal(1, _repository.Count);
    }
}
