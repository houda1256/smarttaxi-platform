namespace SmartTaxi.Domain.Support.Enums;

/// <summary>Not enumerated by the business specification — approved MVP design choice, deliberately distinct wording from SupportIncidentSeverity so the two concepts are never confused.</summary>
public enum SupportTicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}
