using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Professional.Commands.ApproveProfessionalAccountRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Professional.Commands;

public class ApproveProfessionalAccountRequestCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeProfessionalAccountRequestRepository _repository = new();
    private readonly FakeUserDocumentRepository _documentRepository = new();
    private readonly ApproveProfessionalAccountRequestCommandHandler _handler;

    public ApproveProfessionalAccountRequestCommandHandlerTests()
    {
        _handler = new ApproveProfessionalAccountRequestCommandHandler(
            _repository, _userRepository, new DocumentEligibilityChecker(_documentRepository));
    }

    private async Task<Guid> CreateUserAsync()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user.Id;
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
    public async Task Handle_WhenAllRequiredDocumentsApproved_ApprovesRequestAndGrantsRole()
    {
        var userId = await CreateUserAsync();
        await ApproveAllDriverDocumentsAsync(userId);
        var request = new ProfessionalAccountRequest(userId, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ApproveProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, "ok"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(user!.HasRole(UserRole.Driver));
    }

    [Fact]
    public async Task Handle_WhenRequiredDocumentsMissing_ReturnsValidationErrorAndDoesNotGrantRole()
    {
        var userId = await CreateUserAsync();
        var request = new ProfessionalAccountRequest(userId, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ApproveProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.False(user!.HasRole(UserRole.Driver));
        var reloadedRequest = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(Domain.Identity.Professional.Enums.ProfessionalAccountStatus.PendingReview, reloadedRequest!.Status);
    }

    [Fact]
    public async Task Handle_ForAlreadyApprovedRequest_ReturnsConflict()
    {
        var userId = await CreateUserAsync();
        await ApproveAllDriverDocumentsAsync(userId);
        var request = new ProfessionalAccountRequest(userId, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);
        await _handler.Handle(new ApproveProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, null), CancellationToken.None);

        var result = await _handler.Handle(
            new ApproveProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
