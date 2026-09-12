using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>The IsReported flag is toggled via an atomic repository-level guard, not a domain method.</summary>
public sealed class RideMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public RideMessageType MessageType { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsReported { get; private set; }
    public DateTime SentAt { get; private set; }

    private RideMessage()
    {
    }

    public RideMessage(Guid conversationId, Guid senderId, RideMessageType messageType, string content, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Le contenu du message est requis.");
        }

        Id = Guid.NewGuid();
        ConversationId = conversationId;
        SenderId = senderId;
        MessageType = messageType;
        Content = content;
        SentAt = utcNow;
    }
}
