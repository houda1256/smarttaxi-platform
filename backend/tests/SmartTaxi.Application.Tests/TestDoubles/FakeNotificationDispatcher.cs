using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationDispatcher : INotificationDispatcher
{
    public List<NotificationRequest> DispatchedRequests { get; } = [];

    public List<Guid> RetriedDeliveryAttemptIds { get; } = [];

    public Task DispatchAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        DispatchedRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task RetryFailedDeliveryAsync(Guid deliveryAttemptId, CancellationToken cancellationToken)
    {
        RetriedDeliveryAttemptIds.Add(deliveryAttemptId);
        return Task.CompletedTask;
    }
}
