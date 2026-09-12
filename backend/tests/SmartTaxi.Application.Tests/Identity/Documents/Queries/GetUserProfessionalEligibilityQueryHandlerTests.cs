using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Queries.GetUserProfessionalEligibility;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Documents.Queries;

public class GetUserProfessionalEligibilityQueryHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeUserDocumentRepository _documentRepository = new();
    private readonly GetUserProfessionalEligibilityQueryHandler _handler;

    public GetUserProfessionalEligibilityQueryHandlerTests()
    {
        _handler = new GetUserProfessionalEligibilityQueryHandler(
            _userRepository, new DocumentEligibilityChecker(_documentRepository));
    }

    private async Task<Guid> CreateDriverAsync()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Driver, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task Handle_ForDriverMissingAllCriticalDocuments_ReportsNotEligible()
    {
        var userId = await CreateDriverAsync();

        var result = await _handler.Handle(new GetUserProfessionalEligibilityQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var driverEligibility = result.Value!.Single(r => r.Role == UserRole.Driver);
        Assert.False(driverEligibility.IsEligible);
        Assert.Contains(DocumentType.DriverLicense, driverEligibility.MissingOrInvalidTypes);
        Assert.Contains(DocumentType.CriminalRecordCertificate, driverEligibility.MissingOrInvalidTypes);
    }

    [Fact]
    public async Task Handle_ForDriverWithAllCriticalDocumentsApprovedAndValid_ReportsEligible()
    {
        var userId = await CreateDriverAsync();

        foreach (var type in new[] { DocumentType.DriverLicense, DocumentType.CriminalRecordCertificate })
        {
            var document = UserDocument.Upload(userId, type, "key", "f.pdf", "application/pdf", 1, $"hash-{type}", null, null, DateTime.UtcNow);
            await _documentRepository.AddAsync(document, CancellationToken.None);
            await _documentRepository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        }

        var result = await _handler.Handle(new GetUserProfessionalEligibilityQuery(userId), CancellationToken.None);

        var driverEligibility = result.Value!.Single(r => r.Role == UserRole.Driver);
        Assert.True(driverEligibility.IsEligible);
        Assert.Empty(driverEligibility.MissingOrInvalidTypes);
    }

    [Fact]
    public async Task Handle_ForDriverWithExpiredLicense_ReportsNotEligible()
    {
        var userId = await CreateDriverAsync();
        var utcNow = DateTime.UtcNow;

        var license = UserDocument.Upload(
            userId, DocumentType.DriverLicense, "key", "f.pdf", "application/pdf", 1, "hash-1", null, utcNow.AddDays(-1), utcNow.AddDays(-30));
        await _documentRepository.AddAsync(license, CancellationToken.None);
        await _documentRepository.TryApproveAsync(license.Id, Guid.NewGuid(), utcNow.AddDays(-30), null, CancellationToken.None);

        var recordCert = UserDocument.Upload(
            userId, DocumentType.CriminalRecordCertificate, "key2", "f2.pdf", "application/pdf", 1, "hash-2", null, null, utcNow);
        await _documentRepository.AddAsync(recordCert, CancellationToken.None);
        await _documentRepository.TryApproveAsync(recordCert.Id, Guid.NewGuid(), utcNow, null, CancellationToken.None);

        var result = await _handler.Handle(new GetUserProfessionalEligibilityQuery(userId), CancellationToken.None);

        var driverEligibility = result.Value!.Single(r => r.Role == UserRole.Driver);
        Assert.False(driverEligibility.IsEligible);
        Assert.Contains(DocumentType.DriverLicense, driverEligibility.MissingOrInvalidTypes);
        Assert.DoesNotContain(DocumentType.CriminalRecordCertificate, driverEligibility.MissingOrInvalidTypes);
    }

    [Fact]
    public async Task Handle_ForCustomerRole_ReturnsNoEligibilityEntries()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new GetUserProfessionalEligibilityQuery(user.Id), CancellationToken.None);

        Assert.Empty(result.Value!);
    }
}
