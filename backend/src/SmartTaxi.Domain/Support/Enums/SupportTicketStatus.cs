namespace SmartTaxi.Domain.Support.Enums;

/// <summary>Exact statuses from the business specification's Module 10 (Support tickets) lifecycle — none invented, none merged.</summary>
public enum SupportTicketStatus
{
    Open,
    Assigned,
    InProgress,
    WaitingForCustomer,
    Resolved,
    Closed,
    Reopened
}
