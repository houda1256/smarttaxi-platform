using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Documents.Entities;

public class UserDocumentTests
{
    [Fact]
    public void Upload_SetsInitialStateAsFirstPendingVersion()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var document = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "storage-key", "license.pdf", "application/pdf",
            1024, "abc123", issueDate: null, expirationDate: null, utcNow);

        Assert.Equal(userId, document.UserId);
        Assert.Equal(DocumentStatus.Pending, document.Status);
        Assert.Equal(1, document.Version);
        Assert.Null(document.ReplacesDocumentId);
        Assert.Equal(utcNow, document.CreatedAt);
        Assert.Equal(utcNow, document.UpdatedAt);
        Assert.Null(document.ReviewedBy);
        Assert.Null(document.ReviewedAt);
    }

    [Fact]
    public void CreateReplacement_IncrementsVersionAndLinksToOriginal_WithoutMutatingOriginal()
    {
        var utcNow = DateTime.UtcNow;
        var original = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "storage-key-1", "license.pdf", "application/pdf",
            1024, "hash-1", issueDate: null, expirationDate: null, utcNow);

        var replacement = original.CreateReplacement(
            "storage-key-2", "license-v2.pdf", "application/pdf", 2048, "hash-2",
            issueDate: null, expirationDate: null, utcNow.AddDays(1));

        Assert.Equal(2, replacement.Version);
        Assert.Equal(original.Id, replacement.ReplacesDocumentId);
        Assert.Equal(original.UserId, replacement.UserId);
        Assert.Equal(original.DocumentType, replacement.DocumentType);
        Assert.Equal(DocumentStatus.Pending, replacement.Status);
        Assert.NotEqual(original.Id, replacement.Id);

        // The original instance itself is untouched — the repository is
        // responsible for atomically flipping it to Replaced separately.
        Assert.Equal(DocumentStatus.Pending, original.Status);
        Assert.Equal(1, original.Version);
    }

    [Fact]
    public void IsCurrentlyValid_ForApprovedDocumentWithNoExpiration_ReturnsTrue()
    {
        var document = UploadedApprovedDocument(expirationDate: null);

        Assert.True(document.IsCurrentlyValid(DateTime.UtcNow));
    }

    [Fact]
    public void IsCurrentlyValid_ForApprovedDocumentNotYetExpired_ReturnsTrue()
    {
        var utcNow = DateTime.UtcNow;
        var document = UploadedApprovedDocument(expirationDate: utcNow.AddDays(1));

        Assert.True(document.IsCurrentlyValid(utcNow));
    }

    [Fact]
    public void IsCurrentlyValid_ForApprovedDocumentPastExpiration_ReturnsFalse()
    {
        var utcNow = DateTime.UtcNow;
        var document = UploadedApprovedDocument(expirationDate: utcNow.AddDays(-1));

        Assert.False(document.IsCurrentlyValid(utcNow));
    }

    [Theory]
    [InlineData(DocumentStatus.Pending)]
    [InlineData(DocumentStatus.Rejected)]
    [InlineData(DocumentStatus.Expired)]
    [InlineData(DocumentStatus.Replaced)]
    [InlineData(DocumentStatus.Suspended)]
    [InlineData(DocumentStatus.Cancelled)]
    public void IsCurrentlyValid_ForAnyNonApprovedStatus_ReturnsFalse(DocumentStatus status)
    {
        var document = UploadedApprovedDocument(expirationDate: null);
        typeof(UserDocument).GetProperty(nameof(UserDocument.Status))!.SetValue(document, status);

        Assert.False(document.IsCurrentlyValid(DateTime.UtcNow));
    }

    private static UserDocument UploadedApprovedDocument(DateTime? expirationDate)
    {
        var document = UserDocument.Upload(
            Guid.NewGuid(), DocumentType.DriverLicense, "storage-key", "license.pdf", "application/pdf",
            1024, "hash", issueDate: null, expirationDate, DateTime.UtcNow);

        typeof(UserDocument).GetProperty(nameof(UserDocument.Status))!.SetValue(document, DocumentStatus.Approved);
        return document;
    }
}
