using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSupportTicketMessageRepository : ISupportTicketMessageRepository
{
    private readonly List<SupportTicketMessage> _messages = [];

    public Task AddAsync(SupportTicketMessage message, CancellationToken cancellationToken)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SupportTicketMessage>> GetVisibleForRequesterAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SupportTicketMessage> messages = _messages
            .Where(m => m.TicketId == ticketId && !m.IsInternalNote).OrderBy(m => m.CreatedAtUtc).ToList();
        return Task.FromResult(messages);
    }

    public Task<IReadOnlyCollection<SupportTicketMessage>> GetAllForAdminAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<SupportTicketMessage> messages = _messages.Where(m => m.TicketId == ticketId).OrderBy(m => m.CreatedAtUtc).ToList();
        return Task.FromResult(messages);
    }
}
