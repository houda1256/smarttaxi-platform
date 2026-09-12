using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.RenewSubscription;

public sealed record RenewSubscriptionCommand(Guid SubscriptionId, Guid RequestingUserId) : ICommand<Result>;
