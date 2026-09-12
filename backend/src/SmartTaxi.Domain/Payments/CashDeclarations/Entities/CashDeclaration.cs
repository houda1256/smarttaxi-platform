using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.Domain.Payments.CashDeclarations.Entities;

/// <summary>Review/approval/dispute/settlement are atomic repository-level guards, not domain methods — same convention as every other financial status machine here.</summary>
public sealed class CashDeclaration : AggregateRoot
{
    public Guid DriverId { get; private set; }
    public Guid? AssignmentId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public decimal ExpectedCash { get; private set; }
    public decimal DeclaredCash { get; private set; }
    public decimal Difference { get; private set; }
    public CashDeclarationOperatingModel OperatingModel { get; private set; }
    public CashDeclarationStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    private CashDeclaration()
    {
    }

    private CashDeclaration(
        Guid driverId, Guid? assignmentId, DateOnly periodStart, DateOnly periodEnd, decimal expectedCash,
        decimal declaredCash, CashDeclarationOperatingModel operatingModel, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        DriverId = driverId;
        AssignmentId = assignmentId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        ExpectedCash = expectedCash;
        DeclaredCash = declaredCash;
        Difference = declaredCash - expectedCash;
        OperatingModel = operatingModel;
        Status = CashDeclarationStatus.Submitted;
        SubmittedAt = utcNow;
    }

    public static CashDeclaration Submit(
        Guid driverId, Guid? assignmentId, DateOnly periodStart, DateOnly periodEnd, decimal expectedCash,
        decimal declaredCash, CashDeclarationOperatingModel operatingModel, DateTime utcNow)
    {
        if (periodEnd < periodStart)
        {
            throw new ArgumentException("La fin de période ne peut pas précéder le début.");
        }

        if (expectedCash < 0 || declaredCash < 0)
        {
            throw new ArgumentException("Les montants déclarés ne peuvent pas être négatifs.");
        }

        return new CashDeclaration(driverId, assignmentId, periodStart, periodEnd, expectedCash, declaredCash, operatingModel, utcNow);
    }
}
