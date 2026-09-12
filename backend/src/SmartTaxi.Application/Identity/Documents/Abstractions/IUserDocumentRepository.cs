using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Identity.Documents.Abstractions;

public interface IUserDocumentRepository
{
    Task AddAsync(UserDocument document, CancellationToken cancellationToken);

    Task<UserDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserDocument>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserDocument>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Latest (highest-version) non-Replaced document of the given type for a
    /// user, used for duplicate detection and eligibility checks.
    /// </summary>
    Task<UserDocument?> GetLatestForUserAndTypeAsync(
        Guid userId, DocumentType documentType, CancellationToken cancellationToken);

    Task<bool> ExistsWithHashAsync(Guid userId, DocumentType documentType, string sha256, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically approves a Pending document. Returns false if it was no
    /// longer Pending (already reviewed, cancelled, or replaced by a race).
    /// </summary>
    Task<bool> TryApproveAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string rejectionReason, string? reviewComment,
        CancellationToken cancellationToken);

    /// <summary>Suspends a currently Approved document (e.g., discovered fraudulent after the fact).</summary>
    Task<bool> TrySuspendAsync(
        Guid documentId, Guid reviewedBy, DateTime utcNow, string? reviewComment, CancellationToken cancellationToken);

    /// <summary>Cancels a Pending document — owner-only, race-safe against concurrent review.</summary>
    Task<bool> TryCancelAsync(Guid documentId, Guid ownerUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Marks all Approved-but-past-expiration documents as Expired. Returns the count affected.</summary>
    Task<int> ExpireDueDocumentsAsync(DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically flips the current document to Replaced and inserts its
    /// replacement in one transaction. Returns false if the current document
    /// was already Replaced by a race.
    /// </summary>
    Task<bool> TryReplaceAsync(UserDocument current, UserDocument replacement, DateTime utcNow, CancellationToken cancellationToken);
}
