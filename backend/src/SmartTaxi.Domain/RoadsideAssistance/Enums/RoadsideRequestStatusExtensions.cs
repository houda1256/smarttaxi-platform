namespace SmartTaxi.Domain.RoadsideAssistance.Enums;

/// <summary>Single source of truth for which statuses are terminal — never duplicated as inline lists in repositories/handlers (same convention as MaintenanceRequestRepository.TerminalStatuses).</summary>
public static class RoadsideRequestStatusExtensions
{
    public static readonly IReadOnlyCollection<RoadsideRequestStatus> TerminalStatuses =
    [
        RoadsideRequestStatus.Completed,
        RoadsideRequestStatus.Cancelled,
        RoadsideRequestStatus.Expired,
        RoadsideRequestStatus.Disputed
    ];

    public static bool IsTerminal(this RoadsideRequestStatus status) => TerminalStatuses.Contains(status);
}
