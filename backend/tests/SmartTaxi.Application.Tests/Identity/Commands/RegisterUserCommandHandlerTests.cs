using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _handler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, _notificationDispatcher);
    }

    [Fact]
    public async Task Handle_WithValidData_RegistersUserAndReturnsSuccess()
    {
        var command = new RegisterUserCommand("new.user@example.com", "a-valid-password");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new.user@example.com", result.Value!.Email);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
        Assert.Single(_notificationDispatcher.DispatchedRequests, request => request.SourceId == result.Value.UserId);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsValidationFailure()
    {
        var command = new RegisterUserCommand("not-an-email", "a-valid-password");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithAlreadyRegisteredEmail_ReturnsConflictFailure()
    {
        var firstCommand = new RegisterUserCommand("duplicate@example.com", "a-valid-password");
        await _handler.Handle(firstCommand, CancellationToken.None);

        var secondCommand = new RegisterUserCommand("duplicate@example.com", "another-valid-password");
        var result = await _handler.Handle(secondCommand, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithTooShortPassword_ReturnsValidationFailure()
    {
        var command = new RegisterUserCommand("new.user@example.com", "short");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
