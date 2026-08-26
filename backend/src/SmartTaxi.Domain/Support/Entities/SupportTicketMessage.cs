namespace SmartTaxi.Domain.Support.Entities;

/// <summary>
/// Append-only — no edit/delete method exists anywhere in this design, same
/// "plain entity written by the transition/action itself" shape as
/// RideStatusHistory/RoadsidePartnerSelectionHistory. IsInternalNote is set
/// exactly once at construction by whichever command created it
/// (AddTicketMessageCommand always constructs with false; AddInternalNoteCommand
/// always constructs with true) — never a client-suppliable flag. Not an
/// AggregateRoot — it has no independent lifecycle of its own.
/// </summary>
public sealed class SupportTicketMessage
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsInternalNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private SupportTicketMessage()
    {
    }

    private SupportTicketMessage(Guid ticketId, Guid authorUserId, string body, bool isInternalNote, DateTime utcNow)
    {
        Id = Guid.NewGuid();
        TicketId = ticketId;
        AuthorUserId = authorUserId;
        Body = body;
        IsInternalNote = isInternalNote;
        CreatedAtUtc = utcNow;
    }

    public static SupportTicketMessage Create(Guid ticketId, Guid authorUserId, string body, bool isInternalNote, DateTime utcNow)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException("Le ticket est requis.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("L'auteur est requis.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Le contenu du message est requis.");
        }

        return new SupportTicketMessage(ticketId, authorUserId, body.Trim(), isInternalNote, utcNow);
    }
}
