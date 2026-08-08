using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Professional.Commands.SubmitProfessionalAccountRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Professional.Commands;

public class SubmitProfessionalAccountRequestCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeProfessionalAccountRequestRepository _repository = new();
    private readonly SubmitProfessionalAccountRequestCommandHandler _handler;

    public SubmitProfessionalAccountRequestCommandHandlerTests()
    {
        _handler = new SubmitProfessionalAccountRequestCommandHandler(_userRepository, _repository);
    }

    private async Task<Guid> CreateUserAsync()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task Handle_ForSupportedProfessionalRole_CreatesPendingRequest()
    {
        var userId = await CreateUserAsync();

        var result = await _handler.Handle(new SubmitProfessionalAccountRequestCommand(userId, UserRole.Driver), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Handle_ForNonProfessionalRole_ReturnsValidationError()
    {
        var userId = await CreateUserAsync();

        var result = await _handler.Handle(new SubmitProfessionalAccountRequestCommand(userId, UserRole.Customer), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithAlreadyPendingRequestForSameRole_ReturnsConflict()
    {
        var userId = await CreateUserAsync();
        await _handler.Handle(new SubmitProfessionalAccountRequestCommand(userId, UserRole.Driver), CancellationToken.None);

        var result = await _handler.Handle(new SubmitProfessionalAccountRequestCommand(userId, UserRole.Driver), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Handle_ForUnknownUser_ReturnsNotFound()
    {
        var result = await _handler.Handle(
            new SubmitProfessionalAccountRequestCommand(Guid.NewGuid(), UserRole.Driver), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
