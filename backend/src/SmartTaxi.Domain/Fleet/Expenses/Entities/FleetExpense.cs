using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Expenses.Events;
using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;

namespace SmartTaxi.Domain.Fleet.Expenses.Entities;

/// <summary>
/// No final accounting ledger entries here — this is the Fleet-side record
/// only. A clean integration point (OwnerId/FleetId/VehicleId/DriverId +
/// approved Money) is what the future Payments module will consume.
/// </summary>
public sealed class FleetExpense : AggregateRoot
{
    public Guid OwnerId { get; private set; }
    public Guid? FleetId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? DriverId { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public Money Money { get; private set; } = null!;
    public DateOnly ExpenseDate { get; private set; }
    public string? Description { get; private set; }
    public string? ReceiptReference { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FleetExpense()
    {
    }

    private FleetExpense(
        Guid ownerId, Guid? fleetId, Guid? vehicleId, Guid? driverId, ExpenseCategory category, Money money,
        DateOnly expenseDate, string? description, string? receiptReference, Guid createdBy, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        OwnerId = ownerId;
        FleetId = fleetId;
        VehicleId = vehicleId;
        DriverId = driverId;
        Category = category;
        Money = money;
        ExpenseDate = expenseDate;
        Description = description;
        ReceiptReference = receiptReference;
        Status = ExpenseStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = utcNow;

        RaiseDomainEvent(new FleetExpenseCreated(Id, ownerId, utcNow));
    }

    public static FleetExpense Create(
        Guid ownerId, Guid? fleetId, Guid? vehicleId, Guid? driverId, ExpenseCategory category, Money money,
        DateOnly expenseDate, string? description, string? receiptReference, Guid createdBy, DateTime utcNow) =>
        new(ownerId, fleetId, vehicleId, driverId, category, money, expenseDate, description, receiptReference, createdBy, utcNow);
}
