using SmartTaxi.Domain.Fleet.Owners.Entities;
using SmartTaxi.Domain.Fleet.Owners.Enums;
using SmartTaxi.Domain.Fleet.Owners.Events;
using SmartTaxi.Domain.Fleet.Owners.ValueObjects;

namespace SmartTaxi.Domain.Tests.Fleet.Owners.Entities;

public class TaxiOwnerProfileTests
{
    private static Address MakeAddress() => Address.Create("12 Rue Example", "Casablanca", "20000", "Maroc");

    private static BankInformation MakeBankInfo() => BankInformation.Create("Bank", "Holder", "AC12345", "SWIFTXX");

    [Fact]
    public void CreateIndividual_SetsIndividualFieldsAndDefaults()
    {
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var profile = TaxiOwnerProfile.CreateIndividual(
            userId, "Ali", "Ben", "NID123", MakeAddress(), MakeBankInfo(), utcNow);

        Assert.Equal(OwnerType.Individual, profile.OwnerType);
        Assert.Equal("Ali", profile.FirstName);
        Assert.Equal(OwnerVerificationStatus.Pending, profile.VerificationStatus);
        Assert.Equal(OwnerAccountStatus.Active, profile.AccountStatus);
        Assert.Null(profile.LegalName);
        Assert.Single(profile.DomainEvents);
        Assert.IsType<TaxiOwnerProfileCreated>(profile.DomainEvents.Single());
    }

    [Fact]
    public void CreateCompany_SetsCompanyFieldsAndDefaults()
    {
        var profile = TaxiOwnerProfile.CreateCompany(
            Guid.NewGuid(), "Legal SARL", "Trade", "TAX1", "REG1", MakeAddress(), MakeBankInfo(), DateTime.UtcNow);

        Assert.Equal(OwnerType.Company, profile.OwnerType);
        Assert.Equal("Legal SARL", profile.LegalName);
        Assert.Null(profile.FirstName);
    }

    [Fact]
    public void UpdateIndividualDetails_OnCompanyProfile_Throws()
    {
        var profile = TaxiOwnerProfile.CreateCompany(
            Guid.NewGuid(), "Legal", "Trade", "TAX1", "REG1", MakeAddress(), MakeBankInfo(), DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => profile.UpdateIndividualDetails("A", "B", "N", DateTime.UtcNow));
    }

    [Fact]
    public void UpdateContactInformation_ReplacesAddressAndBankInfo()
    {
        var profile = TaxiOwnerProfile.CreateIndividual(
            Guid.NewGuid(), "Ali", "Ben", "NID123", MakeAddress(), MakeBankInfo(), DateTime.UtcNow);
        var newAddress = Address.Create("Other street", "Rabat", null, "Maroc");

        profile.UpdateContactInformation(newAddress, MakeBankInfo(), DateTime.UtcNow);

        Assert.Equal("Rabat", profile.Address.City);
    }
}
