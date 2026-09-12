using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.DataRequests;
using SmartTaxi.Application.Identity.DataRequests.Commands.ProcessPersonalDataRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.DataRequests.Entities;
using SmartTaxi.Domain.Identity.DataRequests.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.DataRequests.Commands;

public class ProcessPersonalDataRequestCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePersonalDataRequestRepository _repository = new();
    private readonly FakeUserDocumentRepository _documentRepository = new();
    private readonly FakeUserPreferencesRepository _preferencesRepository = new();
    private readonly FakeReferralRepository _referralRepository = new();
    private readonly FakeFileStorageService _fileStorage = new();
    private readonly ProcessPersonalDataRequestCommandHandler _handler;

    public ProcessPersonalDataRequestCommandHandlerTests()
    {
        var exportBuilder = new PersonalDataExportBuilder(
            _userRepository, _documentRepository, _preferencesRepository, _referralRepository, _fileStorage);
        _handler = new ProcessPersonalDataRequestCommandHandler(_repository, _userRepository, exportBuilder);
    }

    private async Task<User> CreateUserAsync()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_ApprovingAnExportRequest_ProducesAResultReferenceAndDoesNotDeleteTheUser()
    {
        var user = await CreateUserAsync();
        var request = new PersonalDataRequest(user.Id, PersonalDataRequestType.Export, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, Approve: true, "done"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(PersonalDataRequestStatus.Completed, reloaded!.Status);
        Assert.NotNull(reloaded.ResultReference);
        Assert.Equal(1, _fileStorage.SavedFileCount);
        Assert.NotNull(await _userRepository.GetByIdAsync(user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ApprovingADeletionRequest_AnonymizesAndDeactivatesWithoutDeletingTheRow()
    {
        var user = await CreateUserAsync();
        var originalId = user.Id;
        var request = new PersonalDataRequest(user.Id, PersonalDataRequestType.Deletion, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, Approve: true, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedUser = await _userRepository.GetByIdAsync(originalId, CancellationToken.None);
        Assert.NotNull(reloadedUser);
        Assert.Equal(originalId, reloadedUser!.Id);
        Assert.False(reloadedUser.IsActive);
        Assert.Contains("anonymized", reloadedUser.Email.Value);
    }

    [Fact]
    public async Task Handle_ApprovingAnAnonymizationRequest_ScrubsPiiButKeepsAccountActive()
    {
        var user = await CreateUserAsync();
        var request = new PersonalDataRequest(user.Id, PersonalDataRequestType.Anonymization, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, Approve: true, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.True(reloadedUser!.IsActive);
        Assert.Contains("anonymized", reloadedUser.Email.Value);
    }

    [Fact]
    public async Task Handle_Rejecting_MarksRequestRejectedAndDoesNotTouchTheUser()
    {
        var user = await CreateUserAsync();
        var request = new PersonalDataRequest(user.Id, PersonalDataRequestType.Deletion, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, Approve: false, "insufficient proof"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(PersonalDataRequestStatus.Rejected, reloaded!.Status);
        var reloadedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.True(reloadedUser!.IsActive);
        Assert.DoesNotContain("anonymized", reloadedUser.Email.Value);
    }

    [Fact]
    public async Task Handle_ForAlreadyProcessedRequest_ReturnsConflict()
    {
        var user = await CreateUserAsync();
        var request = new PersonalDataRequest(user.Id, PersonalDataRequestType.Export, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);
        await _handler.Handle(new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, true, null), CancellationToken.None);

        var result = await _handler.Handle(
            new ProcessPersonalDataRequestCommand(Guid.NewGuid(), request.Id, true, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
