using SmartTaxi.Application.Notifications.Contracts;

namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>
/// The single seam other modules call at a business transition — the Application-layer
/// abstraction described by the Module 6 spec. Business handlers depend only on this
/// interface (never on INotificationRepository or any Notifications EF entity), so
/// Notifications' own persistence stays fully encapsulated. Never throws: a failure to
/// notify must never fail the business operation that triggered it (see
/// NotificationDispatcher's own doc comment for how that is guaranteed).
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationRequest request, CancellationToken cancellationToken);

    /// <summary>Re-attempts one previously failed delivery attempt on its original channel. Used by ProcessRetryableDeliveriesCommand.</summary>
    Task RetryFailedDeliveryAsync(Guid deliveryAttemptId, CancellationToken cancellationToken);
}
