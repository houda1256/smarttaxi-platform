using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Professional.Commands.ReactivateProfessionalAccountRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Professional.Commands;

public class ReactivateProfessionalAccountRequestCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeProfessionalAccountRequestRepository _repository = new();
    private readonly FakeUserDocumentRepository _documentRepository = new();
    private readonly ReactivateProfessionalAccountRequestCommandHandler _handler;

    public ReactivateProfessionalAccountRequestCommandHandlerTests()
    {
        _handler = new ReactivateProfessionalAccountRequestCommandHandler(
            _repository, _userRepository, new DocumentEligibilityChecker(_documentRepository));
    }

    private async Task ApproveAllDriverDocumentsAsync(Guid userId)
    {
        foreach (var type in new[] { DocumentType.DriverLicense, DocumentType.CriminalRecordCertificate })
        {
            var document = UserDocument.Upload(userId, type, "key", "f.pdf", "application/pdf", 1, $"hash-{type}", null, null, DateTime.UtcNow);
            await _documentRepository.AddAsync(document, CancellationToken.None);
            await _documentRepository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Handle_ForSuspendedRequestWithStillValidDocuments_ReactivatesAndRegrantsRole()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);
        await ApproveAllDriverDocumentsAsync(user.Id);

        var request = new ProfessionalAccountRequest(user.Id, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);
        await _repository.TryApproveAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        await _repository.TrySuspendAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);

        var result = await _handler.Handle(
            new ReactivateProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, "resolved"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProfessionalAccountStatus.Approved, reloaded!.Status);
        var reloadedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.True(reloadedUser!.HasRole(UserRole.Driver));
    }

    [Fact]
    public async Task Handle_ForSuspendedRequestWithNoLongerValidDocuments_ReturnsValidationError()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var request = new ProfessionalAccountRequest(user.Id, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);
        await _repository.TryApproveAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);
        await _repository.TrySuspendAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);

        var result = await _handler.Handle(
            new ReactivateProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
