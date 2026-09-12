using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Abstractions;

/// <summary>
/// The single place that resolves whether a caller may legitimately reference
/// a given (RelatedEntityType, RelatedEntityId) pair — one branch per closed
/// SupportRelatedEntityType value, each calling the real owning module's own
/// existing read repository (never a second copy of ownership logic). No
/// safe polymorphic DB foreign key exists for this, so referential integrity
/// is enforced here, at the Application layer, exactly once.
/// </summary>
public interface ISupportRelatedEntityValidator
{
    Task<bool> IsValidReferenceAsync(SupportRelatedEntityType type, Guid entityId, Guid callerUserId, CancellationToken cancellationToken);
}
