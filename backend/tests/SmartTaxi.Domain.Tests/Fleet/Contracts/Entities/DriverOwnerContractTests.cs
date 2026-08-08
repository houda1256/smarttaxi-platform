using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Fleet.Contracts.Events;

namespace SmartTaxi.Domain.Tests.Fleet.Contracts.Entities;

public class DriverOwnerContractTests
{
    [Fact]
    public void CreateDraft_ForPercentagePerRideWithValidSplit_Succeeds()
    {
        var contract = DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 60m, 40m, PaymentFrequency.PerRide, null, DateTime.UtcNow);

        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Single(contract.DomainEvents);
        Assert.IsType<OwnerDriverContractCreated>(contract.DomainEvents.Single());
    }

    [Fact]
    public void CreateDraft_ForPercentagePerRideWithInvalidSplit_Throws()
    {
        Assert.Throws<ArgumentException>(() => DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 60m, 60m, PaymentFrequency.PerRide, null, DateTime.UtcNow));
    }

    [Fact]
    public void CreateDraft_ForPercentagePerRideWithOutOfRangeValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.PercentagePerRide, new DateOnly(2026, 1, 1), null,
            null, 120m, -20m, PaymentFrequency.PerRide, null, DateTime.UtcNow));
    }

    [Fact]
    public void CreateDraft_ForFixedSalaryWithoutAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            null, null, null, PaymentFrequency.Monthly, null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateDraftTerms_WhileDraft_Succeeds()
    {
        var contract = DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            1000m, null, null, PaymentFrequency.Monthly, null, DateTime.UtcNow);

        contract.UpdateDraftTerms(
            ContractType.FixedSalary, new DateOnly(2026, 1, 1), null, 1500m, null, null,
            PaymentFrequency.Monthly, "doc-ref", DateTime.UtcNow);

        Assert.Equal(1500m, contract.FixedAmount);
    }

    [Fact]
    public void UpdateDraftTerms_AfterLeavingDraft_Throws()
    {
        var contract = DriverOwnerContract.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            1000m, null, null, PaymentFrequency.Monthly, null, DateTime.UtcNow);
        typeof(DriverOwnerContract).GetProperty(nameof(DriverOwnerContract.Status))!
            .SetValue(contract, ContractStatus.Active);

        Assert.Throws<InvalidOperationException>(() => contract.UpdateDraftTerms(
            ContractType.FixedSalary, new DateOnly(2026, 1, 1), null, 2000m, null, null,
            PaymentFrequency.Monthly, null, DateTime.UtcNow));
    }
}
