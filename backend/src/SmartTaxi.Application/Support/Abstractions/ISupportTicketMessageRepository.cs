using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support.Abstractions;

public interface ISupportTicketMessageRepository
{
    /// <summary>Plain insert — used for admin responses and internal notes, neither of which has a side effect on ticket status (unlike the requester-message path, see ISupportTicketRepository.TryAddRequesterMessageAndAdvanceAsync).</summary>
    Task AddAsync(SupportTicketMessage message, CancellationToken cancellationToken);

    /// <summary>Never includes IsInternalNote==true rows — filtered at the repository itself, not the caller, so a requester-facing query can never accidentally leak an internal note.</summary>
    Task<IReadOnlyCollection<SupportTicketMessage>> GetVisibleForRequesterAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SupportTicketMessage>> GetAllForAdminAsync(Guid ticketId, CancellationToken cancellationToken);
}
