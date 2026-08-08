using SmartTaxi.Domain.Payments.BusinessCustomers.Enums;

namespace SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

/// <summary>
/// AllowedVehicleCategories/AllowedZones are stored as simple comma-separated
/// strings rather than owned collections — a deliberate simplification given
/// the scope of this phase; deactivation is an atomic repository-level guard.
/// </summary>
public sealed class BusinessCustomerEmployee
{
    public Guid Id { get; private set; }
    public Guid BusinessCustomerId { get; private set; }
    public Guid UserId { get; private set; }
    public BusinessEmployeeRole Role { get; private set; }
    public decimal? RideBudgetPerMonth { get; private set; }
    public string? AllowedVehicleCategories { get; private set; }
    public TimeOnly? AllowedScheduleStart { get; private set; }
    public TimeOnly? AllowedScheduleEnd { get; private set; }
    public string? AllowedZones { get; private set; }
    public decimal? PerRideLimit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BusinessCustomerEmployee()
    {
    }

    public BusinessCustomerEmployee(
        Guid businessCustomerId, Guid userId, BusinessEmployeeRole role, decimal? rideBudgetPerMonth,
        string? allowedVehicleCategories, TimeOnly? allowedScheduleStart, TimeOnly? allowedScheduleEnd, string? allowedZones,
        decimal? perRideLimit, DateTime utcNow)
    {
        if (rideBudgetPerMonth is < 0)
        {
            throw new ArgumentException("Le budget mensuel de course ne peut pas être négatif.");
        }

        if (perRideLimit is < 0)
        {
            throw new ArgumentException("La limite par course ne peut pas être négative.");
        }

        Id = Guid.NewGuid();
        BusinessCustomerId = businessCustomerId;
        UserId = userId;
        Role = role;
        RideBudgetPerMonth = rideBudgetPerMonth;
        AllowedVehicleCategories = allowedVehicleCategories;
        AllowedScheduleStart = allowedScheduleStart;
        AllowedScheduleEnd = allowedScheduleEnd;
        AllowedZones = allowedZones;
        PerRideLimit = perRideLimit;
        IsActive = true;
        CreatedAt = utcNow;
    }
}
