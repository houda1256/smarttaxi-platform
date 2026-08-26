using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.ProcessRetryableDeliveries;

/// <summary>No infinite retry: INotificationRetryPolicy.MaxAttempts bounds INotificationDeliveryAttemptRepository.GetRetryableAsync's own filter.</summary>
public sealed class ProcessRetryableDeliveriesCommandHandler : ICommandHandler<ProcessRetryableDeliveriesCommand, Result<int>>
{
    private readonly INotificationDeliveryAttemptRepository _attemptRepository;
    private readonly INotificationRetryPolicy _retryPolicy;
    private readonly INotificationDispatcher _dispatcher;

    public ProcessRetryableDeliveriesCommandHandler(
        INotificationDeliveryAttemptRepository attemptRepository, INotificationRetryPolicy retryPolicy, INotificationDispatcher dispatcher)
    {
        _attemptRepository = attemptRepository;
        _retryPolicy = retryPolicy;
        _dispatcher = dispatcher;
    }

    public async Task<Result<int>> Handle(ProcessRetryableDeliveriesCommand command, CancellationToken cancellationToken)
    {
        var retryable = await _attemptRepository.GetRetryableAsync(DateTime.UtcNow, _retryPolicy.MaxAttempts, cancellationToken);

        foreach (var attempt in retryable)
        {
            await _dispatcher.RetryFailedDeliveryAsync(attempt.Id, cancellationToken);
        }

        return Result<int>.Success(retryable.Count);
    }
}
