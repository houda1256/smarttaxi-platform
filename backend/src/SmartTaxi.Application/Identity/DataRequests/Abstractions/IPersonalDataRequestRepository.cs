using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Identity.DataRequests.Abstractions;

public interface IPersonalDataRequestRepository
{
    Task AddAsync(PersonalDataRequest request, CancellationToken cancellationToken);

    Task<PersonalDataRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PersonalDataRequest>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PersonalDataRequest>> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>Used to block a duplicate submission of the same request type while one is already pending.</summary>
    Task<PersonalDataRequest?> GetPendingForUserAndTypeAsync(
        Guid userId, PersonalDataRequestType requestType, CancellationToken cancellationToken);

    Task<bool> TryCompleteAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, string? resultReference,
        CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(
        Guid requestId, Guid processedBy, DateTime utcNow, string? processingNotes, CancellationToken cancellationToken);
}
