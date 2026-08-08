using SmartTaxi.Application.Identity.Professional.Commands.SuspendProfessionalAccountRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Professional.Entities;
using SmartTaxi.Domain.Identity.Professional.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Professional.Commands;

public class SuspendProfessionalAccountRequestCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeProfessionalAccountRequestRepository _repository = new();
    private readonly SuspendProfessionalAccountRequestCommandHandler _handler;

    public SuspendProfessionalAccountRequestCommandHandlerTests()
    {
        _handler = new SuspendProfessionalAccountRequestCommandHandler(_repository, _userRepository);
    }

    [Fact]
    public async Task Handle_ForApprovedRequest_SuspendsAndRemovesRole()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        user.AssignRole(UserRole.Driver);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var request = new ProfessionalAccountRequest(user.Id, UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);
        await _repository.TryApproveAsync(request.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);

        var result = await _handler.Handle(
            new SuspendProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, "fraude suspectée"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _repository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProfessionalAccountStatus.Suspended, reloadedRequest!.Status);
        var reloadedUser = await _userRepository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.False(reloadedUser!.HasRole(UserRole.Driver));
        Assert.True(reloadedUser.HasRole(UserRole.Customer));
    }

    [Fact]
    public async Task Handle_ForPendingRequest_ReturnsConflict()
    {
        var request = new ProfessionalAccountRequest(Guid.NewGuid(), UserRole.Driver, DateTime.UtcNow);
        await _repository.AddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new SuspendProfessionalAccountRequestCommand(Guid.NewGuid(), request.Id, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
