namespace SmartTaxi.Application.Identity.Documents.Abstractions;

public interface IDocumentExpirationPolicy
{
    /// <summary>How many days before ExpirationDate a document should be flagged as nearing expiration.</summary>
    int ReminderLeadDays { get; }
}
