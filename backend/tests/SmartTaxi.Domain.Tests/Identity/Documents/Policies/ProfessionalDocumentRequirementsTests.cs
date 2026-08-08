using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Documents.Policies;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Tests.Identity.Documents.Policies;

public class ProfessionalDocumentRequirementsTests
{
    [Theory]
    [InlineData(UserRole.Driver)]
    [InlineData(UserRole.TaxiOwner)]
    [InlineData(UserRole.GaragePartner)]
    [InlineData(UserRole.RoadsideAssistancePartner)]
    [InlineData(UserRole.Advertiser)]
    [InlineData(UserRole.BusinessCustomer)]
    public void GetCriticalDocumentTypes_ForEveryProfessionalRole_ReturnsAtLeastOneType(UserRole role)
    {
        var types = ProfessionalDocumentRequirements.GetCriticalDocumentTypes(role);

        Assert.NotEmpty(types);
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Admin)]
    public void GetCriticalDocumentTypes_ForNonProfessionalRole_ReturnsEmpty(UserRole role)
    {
        var types = ProfessionalDocumentRequirements.GetCriticalDocumentTypes(role);

        Assert.Empty(types);
    }

    [Fact]
    public void GetCriticalDocumentTypes_ForDriver_RequiresLicenseAndCriminalRecord()
    {
        var types = ProfessionalDocumentRequirements.GetCriticalDocumentTypes(UserRole.Driver);

        Assert.Contains(DocumentType.DriverLicense, types);
        Assert.Contains(DocumentType.CriminalRecordCertificate, types);
    }
}
