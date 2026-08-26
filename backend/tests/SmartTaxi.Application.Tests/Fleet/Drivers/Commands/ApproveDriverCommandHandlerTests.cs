using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Fleet.Drivers.Commands.ApproveDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Domain.Identity.Documents.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Drivers.Commands;

public class ApproveDriverCommandHandlerTests
{
    private readonly FakeDriverProfileRepository _repository = new();
    private readonly FakeUserDocumentRepository _documentRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly ApproveDriverCommandHandler _handler;

    public ApproveDriverCommandHandlerTests()
    {
        _handler = new ApproveDriverCommandHandler(_repository, new DocumentEligibilityChecker(_documentRepository), _notificationDispatcher);
    }

    private async Task<DriverProfile> CreateUnderReviewProfileAsync(Guid userId)
    {
        var profile = DriverProfile.Create(userId, "LIC1", DateTime.UtcNow.AddYears(2), null, true, DateTime.UtcNow);
        await _repository.AddAsync(profile, CancellationToken.None);
        await _repository.TrySubmitForReviewAsync(profile.Id, DateTime.UtcNow, CancellationToken.None);
        return profile;
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
    public async Task Handle_WhenAllRequiredIdentityDocumentsApproved_ApprovesDriver()
    {
        var userId = Guid.NewGuid();
        var profile = await CreateUnderReviewProfileAsync(userId);
        await ApproveAllDriverDocumentsAsync(userId);

        var result = await _handler.Handle(new ApproveDriverCommand(Guid.NewGuid(), profile.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(profile.Id, CancellationToken.None);
        Assert.Equal(DriverVerificationStatus.Approved, reloaded!.VerificationStatus);
        Assert.Single(_notificationDispatcher.DispatchedRequests, request => request.RecipientUserId == userId);
    }

    [Fact]
    public async Task Handle_WhenRequiredDocumentsMissing_ReturnsValidationErrorAndDoesNotApprove()
    {
        var userId = Guid.NewGuid();
        var profile = await CreateUnderReviewProfileAsync(userId);

        var result = await _handler.Handle(new ApproveDriverCommand(Guid.NewGuid(), profile.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(profile.Id, CancellationToken.None);
        Assert.Equal(DriverVerificationStatus.UnderReview, reloaded!.VerificationStatus);
    }

    [Fact]
    public async Task Handle_WhenReviewerIsTheDriverThemselves_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var profile = await CreateUnderReviewProfileAsync(userId);
        await ApproveAllDriverDocumentsAsync(userId);

        var result = await _handler.Handle(new ApproveDriverCommand(userId, profile.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
