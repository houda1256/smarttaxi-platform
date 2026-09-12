using SmartTaxi.Application.Administration.Commands.ResetUserTwoFactor;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Administration.Commands;

public class ResetUserTwoFactorCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeAdminUserManagementRepository _repository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly ResetUserTwoFactorCommandHandler _handler;

    public ResetUserTwoFactorCommandHandlerTests()
    {
        _handler = new ResetUserTwoFactorCommandHandler(_userRepository, _repository, _notificationDispatcher);
    }

    private async Task<Guid> SeedUserWithTwoFactorAsync(bool twoFactorEnabled = true)
    {
        var user = User.Create(Email.Create("target@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        _repository.SeedUser(user.Id, twoFactorEnabled: twoFactorEnabled);
        return user.Id;
    }

    [Fact]
    public async Task Handle_WithSelfTarget_ReturnsForbidden()
    {
        var adminId = await SeedUserWithTwoFactorAsync();

        var result = await _handler.Handle(new ResetUserTwoFactorCommand(adminId, adminId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_WithUnknownTarget_ReturnsNotFound()
    {
        var result = await _handler.Handle(new ResetUserTwoFactorCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenTwoFactorAlreadyDisabled_ReturnsConflict()
    {
        var targetId = await SeedUserWithTwoFactorAsync(twoFactorEnabled: false);

        var result = await _handler.Handle(new ResetUserTwoFactorCommand(targetId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabled_SucceedsAndSendsMandatorySecurityNotification()
    {
        var targetId = await SeedUserWithTwoFactorAsync();

        var result = await _handler.Handle(new ResetUserTwoFactorCommand(targetId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var notification = Assert.Single(_notificationDispatcher.DispatchedRequests);
        Assert.Equal(targetId, notification.RecipientUserId);
        Assert.True(notification.IsMandatory);
        Assert.Equal("security.two-factor.admin-reset", notification.TemplateKey);
    }
}
