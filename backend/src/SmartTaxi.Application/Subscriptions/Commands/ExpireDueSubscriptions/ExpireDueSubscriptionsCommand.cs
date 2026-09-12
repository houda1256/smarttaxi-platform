using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Subscriptions.Commands.ExpireDueSubscriptions;

/// <summary>
/// Manual/admin-triggered expiration sweep — there is no background job
/// scheduler in this codebase yet, so this is the interim mechanism the spec's
/// "when EndDate is reached" rule runs through until one exists (documented as
/// a known limitation in the phase report). Entitlement checks never trust a
/// stale Active Status past EndDate regardless (see Subscription.IsEffectivelyActive),
/// so this sweep is a cleanup pass, not the only enforcement point.
/// </summary>
public sealed record ExpireDueSubscriptionsCommand : ICommand<Result<int>>;
