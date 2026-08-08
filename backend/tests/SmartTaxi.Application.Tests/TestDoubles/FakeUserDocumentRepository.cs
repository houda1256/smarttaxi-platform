using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>
/// Reflection-based mutation simulates the same all-or-nothing atomicity the
/// real repository achieves via SQL ExecuteUpdateAsync — intentional, not a
/// hack (consistent with the other Fake*Repository doubles in this project).
/// True concurrent-race behavior is covered by Postgres integration tests.
/// </summary>
public sealed class FakeUserDocumentRepository : IUserDocumentRepository
{
    private readonly Dictionary<Guid, UserDocument> _documentsById = new();

    public Task AddAsync(UserDocument document, CancellationToken cancellationToken)
    {
        _documentsById[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<UserDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(_documentsById.GetValueOrDefault(documentId));

    public Task<IReadOnlyCollection<UserDocument>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserDocument> documents = _documentsById.Values.Where(d => d.UserId == userId).ToList();
        return Task.FromResult(documents);
    }

    public Task<IReadOnlyCollection<UserDocument>> GetPendingAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UserDocument> documents =
            _documentsById.Values.Where(d => d.Status == DocumentStatus.Pending).ToList();
        return Task.FromResult(documents);
    }

    public Task<UserDocument?> GetLatestForUserAndTypeAsync(
        Guid userId, DocumentType documentType, CancellationToken cancellationToken)
    {
        var latest = _documentsById.Values
            .Where(d => d.UserId == userId && d.DocumentType == documentType && d.Status != DocumentStatus.Replaced)
            .OrderByDescending(d => d.Version)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<bool> ExistsWithHashAsync(
        Guid userId, DocumentType documentType, string sha256, CancellationToken cancellationToken)
    {
        var exists = _documentsById.Values.Any(d =>
            d.UserId == userId && d.DocumentType == documentType && d.Sha256 == sha256
            && d.Status != DocumentStatus.Replaced && d.Status != DocumentStatus.Cancelled);

        return Task.FromResult(exists);
    }

    public Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != DocumentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(UserDocument.Status), DocumentStatus.Approved);
        SetProperty(document, nameof(UserDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(UserDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(UserDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(UserDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != DocumentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(UserDocument.Status), DocumentStatus.Rejected);
        SetProperty(document, nameof(UserDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(UserDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(UserDocument.RejectionReason), rejectionReason);
        SetProperty(document, nameof(UserDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(UserDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document) || document.Status != DocumentStatus.Approved)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(UserDocument.Status), DocumentStatus.Suspended);
        SetProperty(document, nameof(UserDocument.ReviewedBy), reviewedBy);
        SetProperty(document, nameof(UserDocument.ReviewedAt), utcNow);
        SetProperty(document, nameof(UserDocument.ReviewComment), reviewComment);
        SetProperty(document, nameof(UserDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryCancelAsync(Guid documentId, Guid ownerUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(documentId, out var document)
            || document.UserId != ownerUserId
            || document.Status != DocumentStatus.Pending)
        {
            return Task.FromResult(false);
        }

        SetProperty(document, nameof(UserDocument.Status), DocumentStatus.Cancelled);
        SetProperty(document, nameof(UserDocument.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var due = _documentsById.Values
            .Where(d => d.Status == DocumentStatus.Approved && d.ExpirationDate is not null && d.ExpirationDate <= utcNow)
            .ToList();

        foreach (var document in due)
        {
            SetProperty(document, nameof(UserDocument.Status), DocumentStatus.Expired);
            SetProperty(document, nameof(UserDocument.UpdatedAt), utcNow);
        }

        return Task.FromResult(due.Count);
    }

    public Task<bool> TryReplaceAsync(
        UserDocument current, UserDocument replacement, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_documentsById.TryGetValue(current.Id, out var existing) || existing.Status == DocumentStatus.Replaced)
        {
            return Task.FromResult(false);
        }

        SetProperty(existing, nameof(UserDocument.Status), DocumentStatus.Replaced);
        SetProperty(existing, nameof(UserDocument.UpdatedAt), utcNow);
        _documentsById[replacement.Id] = replacement;
        return Task.FromResult(true);
    }

    public int Count => _documentsById.Count;

    private static void SetProperty(UserDocument document, string propertyName, object? value) =>
        typeof(UserDocument).GetProperty(propertyName)!.SetValue(document, value);
}
