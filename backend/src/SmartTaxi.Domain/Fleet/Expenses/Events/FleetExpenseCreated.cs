using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Expenses.Events;

public sealed record FleetExpenseCreated(Guid ExpenseId, Guid OwnerId, DateTime OccurredAtUtc) : IDomainEvent;
