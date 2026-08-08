using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Fleet.Contracts.Events;

namespace SmartTaxi.Domain.Fleet.Contracts.Entities;

/// <summary>
/// ContractType is the discriminator for how to interpret the flat financial
/// fields below it (FixedAmount for salary/rental types, DriverPercentage +
/// OwnerPercentage for PercentagePerRide) — there is no separate
/// "RevenueSharingRule" concept beyond this. Status transitions
/// (activate/suspend/terminate) are atomic repository-level guards. Financial
/// terms are immutable once the contract leaves Draft — the only way to
/// change them is to terminate this contract and create a new one (a
/// versioned amendment), never an in-place edit, per instructions.
/// </summary>
public sealed class DriverOwnerContract : AggregateRoot
{
    public Guid OwnerId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public ContractType ContractType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? DriverPercentage { get; private set; }
    public decimal? OwnerPercentage { get; private set; }
    public PaymentFrequency PaymentFrequency { get; private set; }
    public string? DocumentReference { get; private set; }
    public ContractStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DriverOwnerContract()
    {
    }

    private DriverOwnerContract(
        Guid ownerId, Guid driverId, Guid? vehicleId, ContractType contractType, DateOnly startDate, DateOnly? endDate,
        decimal? fixedAmount, decimal? driverPercentage, decimal? ownerPercentage, PaymentFrequency paymentFrequency,
        string? documentReference, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ValidateFinancialTerms(contractType, fixedAmount, driverPercentage, ownerPercentage);

        if (endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("La date de fin ne peut pas précéder la date de début.");
        }

        OwnerId = ownerId;
        DriverId = driverId;
        VehicleId = vehicleId;
        ContractType = contractType;
        StartDate = startDate;
        EndDate = endDate;
        FixedAmount = fixedAmount;
        DriverPercentage = driverPercentage;
        OwnerPercentage = ownerPercentage;
        PaymentFrequency = paymentFrequency;
        DocumentReference = documentReference;
        Status = ContractStatus.Draft;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new OwnerDriverContractCreated(Id, ownerId, driverId, utcNow));
    }

    public static DriverOwnerContract CreateDraft(
        Guid ownerId, Guid driverId, Guid? vehicleId, ContractType contractType, DateOnly startDate, DateOnly? endDate,
        decimal? fixedAmount, decimal? driverPercentage, decimal? ownerPercentage, PaymentFrequency paymentFrequency,
        string? documentReference, DateTime utcNow) =>
        new(ownerId, driverId, vehicleId, contractType, startDate, endDate, fixedAmount, driverPercentage,
            ownerPercentage, paymentFrequency, documentReference, utcNow);

    /// <summary>Financial terms can only be edited while still Draft — immutable from PendingSignature onward.</summary>
    public void UpdateDraftTerms(
        ContractType contractType, DateOnly startDate, DateOnly? endDate, decimal? fixedAmount,
        decimal? driverPercentage, decimal? ownerPercentage, PaymentFrequency paymentFrequency,
        string? documentReference, DateTime utcNow)
    {
        if (Status != ContractStatus.Draft)
        {
            throw new InvalidOperationException(
                "Les termes financiers d'un contrat actif sont immuables ; créez un nouveau contrat pour un avenant.");
        }

        ValidateFinancialTerms(contractType, fixedAmount, driverPercentage, ownerPercentage);

        if (endDate is not null && endDate < startDate)
        {
            throw new ArgumentException("La date de fin ne peut pas précéder la date de début.");
        }

        ContractType = contractType;
        StartDate = startDate;
        EndDate = endDate;
        FixedAmount = fixedAmount;
        DriverPercentage = driverPercentage;
        OwnerPercentage = ownerPercentage;
        PaymentFrequency = paymentFrequency;
        DocumentReference = documentReference;
        UpdatedAt = utcNow;
    }

    private static void ValidateFinancialTerms(
        ContractType contractType, decimal? fixedAmount, decimal? driverPercentage, decimal? ownerPercentage)
    {
        if (contractType == ContractType.PercentagePerRide)
        {
            if (driverPercentage is null || ownerPercentage is null)
            {
                throw new ArgumentException("Les pourcentages chauffeur et propriétaire sont requis pour ce type de contrat.");
            }

            if (driverPercentage < 0 || driverPercentage > 100 || ownerPercentage < 0 || ownerPercentage > 100)
            {
                throw new ArgumentException("Les pourcentages doivent être compris entre 0 et 100.");
            }

            if (driverPercentage + ownerPercentage != 100)
            {
                throw new ArgumentException("La somme des pourcentages chauffeur et propriétaire doit être égale à 100.");
            }
        }
        else if (contractType is ContractType.FixedSalary or ContractType.DailyRental or ContractType.WeeklyRental
                 or ContractType.MonthlyRental)
        {
            if (fixedAmount is null || fixedAmount < 0)
            {
                throw new ArgumentException("Un montant fixe positif est requis pour ce type de contrat.");
            }
        }
    }
}
