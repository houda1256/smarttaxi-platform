using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Fleet.Expenses.Events;

public sealed record FleetExpenseApproved(Guid ExpenseId, Guid ApprovedBy, DateTime OccurredAtUtc) : IDomainEvent;
